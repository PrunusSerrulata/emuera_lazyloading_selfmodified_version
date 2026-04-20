using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.UI.Game.Image;

static class AppContents
{
	static ConcurrentDictionary<int, GraphicsImage> gList = [];

	private static readonly int MAX_LRU_CAPACITY = 800;
	private static LinkedList<string> fileLruList = new LinkedList<string>();
	private static Dictionary<string, LinkedListNode<string>> fileLruNodes = new();
	private static ConcurrentDictionary<string, LoadedFileInfo> fileLruCache = new();
	private static ConcurrentDictionary<string, string> spriteToFilepath = new(Config.StrComper);

	private static SqliteConnection metaDb;

	public static HashSet<ConstImage> tempLoadedConstImages = [];
	public static HashSet<GraphicsImage> tempLoadedGraphicsImages = [];

	private class LoadedFileInfo
	{
		public SKBitmap Bitmap;
		public List<(SKBitmap Bitmap, int Delay)> AnimFrames;
		public HashSet<string> SpriteNames = new();
		public int RefCount => SpriteNames.Count;
	}

	static AppContents()
	{
		metaDb = new SqliteConnection("Data Source=:memory:");
		metaDb.Open();
		using var cmd = metaDb.CreateCommand();
		cmd.CommandText = @"
			CREATE TABLE SpriteMeta (
				Name TEXT,
				FrameIndex INTEGER,
				FilePath TEXT,
				RectX INTEGER, RectY INTEGER, RectW INTEGER, RectH INTEGER,
				PosX INTEGER, PosY INTEGER,
				Delay INTEGER,
				DestW INTEGER, DestH INTEGER,
				IsAnime INTEGER,
				PRIMARY KEY (Name, FrameIndex)
			)";
		cmd.ExecuteNonQuery();
	}

	static public GraphicsImage GetGraphics(int i)
	{
		if (gList.TryGetValue(i, out GraphicsImage value))
			return value;
		GraphicsImage g = new(i);
		gList[i] = g;
		return g;
	}

	static public ASprite GetSprite(string name)
	{
		if (string.IsNullOrEmpty(name))
			return null;
		name = name.ToUpper(CultureInfo.InvariantCulture);

		if (activeSprites.TryGetValue(name, out ASprite sprite))
		{
			if (spriteToFilepath.TryGetValue(name, out string filepath))
				UpdateFileLRU(filepath);
			return sprite;
		}

		return LoadSpriteFromMeta(name);
	}

	private static void UpdateFileLRU(string filepath)
	{
		if (string.IsNullOrEmpty(filepath))
			return;
		lock (fileLruList)
		{
			if (fileLruNodes.TryGetValue(filepath, out var node))
			{
				if (node.List != null)
					fileLruList.Remove(node);
				fileLruList.AddLast(node);
			}
			else
			{
				node = fileLruList.AddLast(filepath);
				fileLruNodes[filepath] = node;
			}
		}
	}

	private static void EnforceFileLRUCapacity()
	{
		lock (fileLruList)
		{
			while (fileLruList.Count > MAX_LRU_CAPACITY)
			{
				var oldestNode = fileLruList.First;
				string oldest = oldestNode.Value;
				fileLruList.RemoveFirst();
				fileLruNodes.Remove(oldest);

				if (fileLruCache.TryRemove(oldest, out LoadedFileInfo info))
				{
					info.Bitmap?.Dispose();
					if (info.AnimFrames != null)
					{
						foreach (var frame in info.AnimFrames)
						{
							frame.Bitmap?.Dispose();
						}
					}
					foreach (var spriteName in info.SpriteNames)
					{
						activeSprites.TryRemove(spriteName, out _);
						spriteToFilepath.TryRemove(spriteName, out _);
					}
				}
			}
		}
	}

	private static List<(SKBitmap Bitmap, int Delay)> LoadOrGetAnimFrames(string filepath)
	{
		if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
			return null;

		if (fileLruCache.TryGetValue(filepath, out LoadedFileInfo info))
		{
			if (info.AnimFrames != null)
				return info.AnimFrames;
		}

		var animFrames = AnimatedImageHelper.Decode(filepath);
		if (animFrames == null)
			return null;

		if (!fileLruCache.TryGetValue(filepath, out info))
		{
			info = new LoadedFileInfo { SpriteNames = new HashSet<string>() };
			fileLruCache[filepath] = info;
		}
		info.AnimFrames = animFrames;
		return animFrames;
	}

	private class MetaRow
	{
		public string FilePath;
		public Rectangle Rect;
		public Point Pos;
		public int Delay;
		public Size DestSize;
		public bool IsAnimeHeader;
	}

	private static ASprite LoadSpriteFromMeta(string name)
	{
		using var cmd = metaDb.CreateCommand();
		cmd.CommandText = "SELECT * FROM SpriteMeta WHERE Name = @name ORDER BY FrameIndex ASC";
		cmd.Parameters.AddWithValue("@name", name);

		using var reader = cmd.ExecuteReader();
		List<MetaRow> rows = new List<MetaRow>();
		while (reader.Read())
		{
			rows.Add(new MetaRow
			{
				FilePath = reader.GetString(2),
				Rect = new Rectangle(reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)),
				Pos = new Point(reader.GetInt32(7), reader.GetInt32(8)),
				Delay = reader.GetInt32(9),
				DestSize = new Size(reader.GetInt32(10), reader.GetInt32(11)),
				IsAnimeHeader = reader.GetInt32(12) == 1
			});
		}
		reader.Close();

		if (rows.Count == 0)
			return null;

		ASprite newSprite = null;
		string primaryFilepath = rows[0].FilePath;

		// 模式1：旧版多行 ANIME 定义 (第一行是 Header)
		if (rows[0].IsAnimeHeader)
		{
			Size destSize = rows[0].DestSize;
			SpriteAnime anime = new SpriteAnime(name, destSize);
			
			for (int i = 1; i < rows.Count; i++)
			{
				var r = rows[i];
				if (!File.Exists(r.FilePath)) continue;
				
				SKBitmap bmp = LoadOrGetFileBitmap(r.FilePath);
				if (bmp == null) continue;

				ConstImage img = new ConstImage($"{name}_F{i}");
				img.CreateFrom(bmp.Copy(), r.FilePath, false);
				
				Rectangle fRect = r.Rect.Width == 0 ? new Rectangle(0, 0, bmp.Width, bmp.Height) : r.Rect;
				anime.AddFrame(img, fRect, r.Pos, r.Delay);
				
				// 注册 LRU 依赖
				if (fileLruCache.TryGetValue(r.FilePath, out LoadedFileInfo info))
					info.SpriteNames.Add(name);
				UpdateFileLRU(r.FilePath);
			}
			newSprite = anime;
		}
		// 模式2 & 模式3：单行定义（可能是静态图，也可能是单文件 WebP/GIF 动图）
		else
		{
			var r = rows[0];
			if (!File.Exists(r.FilePath)) return null;

			// 尝试作为动图解码
			var animFrames = LoadOrGetAnimFrames(r.FilePath);
			
			// 模式2：单文件多帧动图 (WebP/GIF)
			if (animFrames != null && animFrames.Count > 1)
			{
				// 使用CSV中定义的画框宽度和高度，如果没有定义则使用裁剪区域的大小或动图帧大小
				Size destSize;
				if (r.DestSize.Width > 0 && r.DestSize.Height > 0)
				{
					destSize = r.DestSize;
				}
				else if (r.Rect.Width > 0)
				{
					destSize = r.Rect.Size;
				}
				else
				{
					destSize = new Size(animFrames[0].Bitmap.Width, animFrames[0].Bitmap.Height);
				}
				SpriteAnime anime = new SpriteAnime(name, destSize);
				
				for (int i = 0; i < animFrames.Count; i++)
				{
					var frame = animFrames[i];
					ConstImage frameImg = new ConstImage($"{name}_F{i}");
					frameImg.CreateFrom(frame.Bitmap.Copy(), r.FilePath, false);
					
					// 如果 CSV 定义了裁剪，应用到每一帧；否则使用整帧尺寸
					Rectangle fRect = r.Rect.Width == 0 ? new Rectangle(0, 0, frame.Bitmap.Width, frame.Bitmap.Height) : r.Rect;
					int fDelay = r.Delay > 0 ? r.Delay : frame.Delay;
					
					anime.AddFrame(frameImg, fRect, r.Pos, fDelay);
				}
				newSprite = anime;
			}
			// 模式3：普通单帧静态图
			else
			{
				SKBitmap fileBitmap = LoadOrGetFileBitmap(r.FilePath);
				if (fileBitmap != null)
				{
					ConstImage img = new ConstImage(name + "_BASE");
					img.CreateFrom(fileBitmap.Copy(), r.FilePath, false);
					Rectangle fRect = r.Rect.Width == 0 ? new Rectangle(0, 0, fileBitmap.Width, fileBitmap.Height) : r.Rect;
					// 使用CSV中定义的画框宽度和高度，如果没有定义则使用裁剪区域的大小
					Size destSize = (r.DestSize.Width > 0 && r.DestSize.Height > 0) ? r.DestSize : fRect.Size;
					newSprite = new SpriteF(name, img, fRect, r.Pos, destSize);
				}
			}

			// 注册 LRU 依赖
			if (newSprite != null)
			{
				if (fileLruCache.TryGetValue(r.FilePath, out LoadedFileInfo info))
					info.SpriteNames.Add(name);
				UpdateFileLRU(r.FilePath);
			}
		}

		if (newSprite != null)
		{
			activeSprites[name] = newSprite;
			spriteToFilepath[name] = primaryFilepath;
			EnforceFileLRUCapacity();
		}

		return newSprite;
	}

	private static SKBitmap LoadOrGetFileBitmap(string filepath)
	{
		if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
			return null;

		if (fileLruCache.TryGetValue(filepath, out LoadedFileInfo info))
		{
			if (info.Bitmap != null)
				return info.Bitmap;
		}

		var skbmp = SKBitmap.Decode(filepath);
		if (skbmp == null)
			return null;

		if (!fileLruCache.TryGetValue(filepath, out info))
		{
			info = new LoadedFileInfo { SpriteNames = new HashSet<string>() };
			fileLruCache[filepath] = info;
		}

		info.Bitmap = skbmp;
		return info.Bitmap;
	}

	static public void CreateSpriteG(string imgName, GraphicsImage parent, Rectangle rect, Point pos, Size destSize)
	{
		if (string.IsNullOrEmpty(imgName))
			return;
		imgName = imgName.ToUpper(CultureInfo.InvariantCulture);
		SpriteG newSprite = new SpriteG(imgName, parent, rect, pos, destSize);
		activeSprites[imgName] = newSprite;
	}

	static public void CreateSpriteAnime(string imgName, int w, int h)
	{
		if (string.IsNullOrEmpty(imgName))
			return;
		imgName = imgName.ToUpper(CultureInfo.InvariantCulture);
		SpriteAnime anime = new SpriteAnime(imgName, new Size(w, h));
		activeSprites[imgName] = anime;
	}

	public static bool CreateSpriteFromFileDynamic(string name, string filepath)
	{
		if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(filepath))
			return false;
		name = name.ToUpper(CultureInfo.InvariantCulture);

		if (activeSprites.TryGetValue(name, out _))
			return true;

		if (!File.Exists(filepath))
			return false;

		ASprite newSprite = null;
		var animFrames = LoadOrGetAnimFrames(filepath);
		if (animFrames != null && animFrames.Count > 1)
		{
			Size animeSize = new Size(animFrames[0].Bitmap.Width, animFrames[0].Bitmap.Height);
			SpriteAnime anime = new SpriteAnime(name, animeSize);
			for (int i = 0; i < animFrames.Count; i++)
			{
				var frame = animFrames[i];
				ConstImage frameImg = new ConstImage($"{name}_F{i}");
				frameImg.CreateFrom(frame.Bitmap.Copy(), filepath, false);
				Rectangle fRect = new Rectangle(0, 0, frame.Bitmap.Width, frame.Bitmap.Height);
				anime.AddFrame(frameImg, fRect, Point.Empty, frame.Delay);
			}
			newSprite = anime;
		}
		else
		{
			SKBitmap fileBitmap = LoadOrGetFileBitmap(filepath);
			if (fileBitmap == null)
				return false;
			if (fileBitmap.Width > AbstractImage.MAX_IMAGESIZE || fileBitmap.Height > AbstractImage.MAX_IMAGESIZE)
				return false;

			ConstImage img = new ConstImage(name + "_DYN");
			img.CreateFrom(fileBitmap.Copy(), filepath, false);
			newSprite = new SpriteF(name, img, new Rectangle(0, 0, fileBitmap.Width, fileBitmap.Height), Point.Empty, new Size(fileBitmap.Width, fileBitmap.Height));
		}

		if (newSprite != null)
		{
			activeSprites[name] = newSprite;
			spriteToFilepath[name] = filepath;

			if (fileLruCache.TryGetValue(filepath, out LoadedFileInfo info))
			{
				info.SpriteNames.Add(name);
			}

			UpdateFileLRU(filepath);
			EnforceFileLRUCapacity();
			return true;
		}
		return false;
	}

	static public bool GetSprite_OnlyCheckExists(string name)
	{
		if (string.IsNullOrEmpty(name))
			return false;
		name = name.ToUpper(CultureInfo.InvariantCulture);

		if (activeSprites.ContainsKey(name))
			return true;

		using var cmd = metaDb.CreateCommand();
		cmd.CommandText = "SELECT Name FROM SpriteMeta WHERE Name = @name";
		cmd.Parameters.AddWithValue("@name", name);
		using var reader = cmd.ExecuteReader();
		return reader.Read();
	}

	static public void SpriteDispose(string name)
	{
		if (string.IsNullOrEmpty(name))
			return;
		name = name.ToUpper(CultureInfo.InvariantCulture);
		if (activeSprites.TryRemove(name, out ASprite sprite))
		{
			sprite.Dispose();

			if (spriteToFilepath.TryRemove(name, out string filepath))
			{
				if (fileLruCache.TryGetValue(filepath, out LoadedFileInfo info))
				{
					info.SpriteNames.Remove(name);
				}
			}
		}
	}

	static public long SpriteDisposeAll(bool delCsvImage)
	{
		int sprites = activeSprites.Count;
		foreach (var s in activeSprites.Values)
			s.Dispose();
		activeSprites.Clear();
		spriteToFilepath.Clear();

		foreach (var info in fileLruCache.Values)
		{
			info.Bitmap?.Dispose();
			if (info.AnimFrames != null)
			{
				foreach (var f in info.AnimFrames) f.Bitmap?.Dispose();
			}
			info.SpriteNames.Clear();
		}
		fileLruCache.Clear();
		lock (fileLruList)
		{
			fileLruList.Clear();
			fileLruNodes.Clear();
		}
		return sprites;
	}

	static public Exception LoadContents(bool reload)
	{
		if (!Directory.Exists(Program.ContentDir))
			return null;
		try
		{
			if (reload)
			{
				using var clearCmd = metaDb.CreateCommand();
				clearCmd.CommandText = "DELETE FROM SpriteMeta";
				clearCmd.ExecuteNonQuery();

				foreach (var s in activeSprites.Values)
					s.Dispose();
				activeSprites.Clear();
				spriteToFilepath.Clear();

				foreach (var info in fileLruCache.Values)
				{
					info.Bitmap?.Dispose();
					if (info.AnimFrames != null)
					{
						foreach (var f in info.AnimFrames) f.Bitmap?.Dispose();
					}
					info.SpriteNames.Clear();
				}
				fileLruCache.Clear();
				lock (fileLruList)
				{
					fileLruList.Clear();
					fileLruNodes.Clear();
				}
			}

			var csvFiles = Directory.EnumerateFiles(Program.ContentDir, "*.csv", SearchOption.AllDirectories);
			using var trans = metaDb.BeginTransaction();
			using var insertCmd = metaDb.CreateCommand();
			insertCmd.Transaction = trans;
			insertCmd.CommandText = @"INSERT OR REPLACE INTO SpriteMeta 
				(Name, FrameIndex, FilePath, RectX, RectY, RectW, RectH, PosX, PosY, Delay, DestW, DestH, IsAnime) 
				VALUES (@name, @idx, @filepath, @rx, @ry, @rw, @rh, @px, @py, @delay, @dw, @dh, @isAnime)";

			var pName = insertCmd.Parameters.Add("@name", SqliteType.Text);
			var pIdx = insertCmd.Parameters.Add("@idx", SqliteType.Integer);
			var pPath = insertCmd.Parameters.Add("@filepath", SqliteType.Text);
			var pRx = insertCmd.Parameters.Add("@rx", SqliteType.Integer);
			var pRy = insertCmd.Parameters.Add("@ry", SqliteType.Integer);
			var pRw = insertCmd.Parameters.Add("@rw", SqliteType.Integer);
			var pRh = insertCmd.Parameters.Add("@rh", SqliteType.Integer);
			var pPx = insertCmd.Parameters.Add("@px", SqliteType.Integer);
			var pPy = insertCmd.Parameters.Add("@py", SqliteType.Integer);
			var pDelay = insertCmd.Parameters.Add("@delay", SqliteType.Integer);
			var pDw = insertCmd.Parameters.Add("@dw", SqliteType.Integer);
			var pDh = insertCmd.Parameters.Add("@dh", SqliteType.Integer);
			var pIsAnime = insertCmd.Parameters.Add("@isAnime", SqliteType.Integer);

			foreach (var path in csvFiles)
			{
				string directory = Path.GetDirectoryName(path) + "\\";
				string[] lines = File.ReadAllLines(path, EncodingHandler.DetectEncoding(path));

				Dictionary<string, int> frameCounters = new(Config.StrComper);

				foreach (var line in lines)
				{
					string str = line.Trim();
					if (str.Length == 0 || str.StartsWith(';'))
						continue;
					string[] tokens = str.Split(',');
					if (tokens.Length < 2)
						continue;

					string name = tokens[0].Trim().ToUpper(CultureInfo.InvariantCulture);
					string arg2 = tokens[1].Trim();

					pName.Value = name;
					pPath.Value = "";
					pRx.Value = 0; pRy.Value = 0; pRw.Value = 0; pRh.Value = 0;
					pPx.Value = 0; pPy.Value = 0; pDelay.Value = 0;
					pDw.Value = 0; pDh.Value = 0; pIsAnime.Value = 0;

					// 纯粹的文本解析和入库，绝对不加载图片
					if (arg2.Equals("ANIME", StringComparison.OrdinalIgnoreCase))
					{
						if (tokens.Length >= 4)
						{
							int.TryParse(tokens[2], out int width);
							int.TryParse(tokens[3], out int height);
							if (width > 0 && height > 0)
							{
								pIdx.Value = -1; // Header marker
								pDw.Value = width;
								pDh.Value = height;
								pIsAnime.Value = 1;
								insertCmd.ExecuteNonQuery();
								frameCounters[name] = 0;
							}
						}
						continue;
					}

					string fullPath = directory + arg2;
					pPath.Value = fullPath;

					if (tokens.Length >= 6)
					{
						int.TryParse(tokens[2], out int rx); pRx.Value = rx;
						int.TryParse(tokens[3], out int ry); pRy.Value = ry;
						int.TryParse(tokens[4], out int rw); pRw.Value = rw;
						int.TryParse(tokens[5], out int rh); pRh.Value = rh;
					}
					if (tokens.Length >= 8)
					{
						int.TryParse(tokens[6], out int px); pPx.Value = px;
						int.TryParse(tokens[7], out int py); pPy.Value = py;
					}
					if (tokens.Length >= 9)
					{
						int.TryParse(tokens[8], out int delay); pDelay.Value = delay;
					}
					// 处理画框宽度和高度参数
					if (tokens.Length >= 11)
					{
						int.TryParse(tokens[9], out int destW); pDw.Value = destW;
						int.TryParse(tokens[10], out int destH); pDh.Value = destH;
					}

					if (!frameCounters.ContainsKey(name))
						frameCounters[name] = 0;

					pIdx.Value = frameCounters[name]++;
					insertCmd.ExecuteNonQuery();
				}
			}
			trans.Commit();
		}
		catch (Exception e)
		{
			return e;
		}
		return null;
	}

	static public void UnloadContents()
	{
		SpriteDisposeAll(true);
		foreach (var graph in gList.Values)
			graph.GDispose();
		gList.Clear();
        
		// 新增：清理动画缓存
		AnimatedImageHelper.ClearCache();
	}

	static public void UnloadGraphicList()
	{
		foreach (var graph in gList.Values)
			graph.GDispose();
		gList.Clear();
	}

	static public void UnloadTempLoadedConstImageNames()
	{
		lock (tempLoadedConstImages)
		{
			foreach (ConstImage img in tempLoadedConstImages)
				img.Dispose();
			tempLoadedConstImages.Clear();
		}
	}

	static public void UnloadTempLoadedGraphicsImageNames()
	{
		lock (tempLoadedGraphicsImages)
		{
			foreach (GraphicsImage img in tempLoadedGraphicsImages)
				if (img.useImgList)
					img.UnLoad();
			tempLoadedGraphicsImages.Clear();
		}
	}

	private static ConcurrentDictionary<string, ASprite> activeSprites = new(Config.StrComper);
}