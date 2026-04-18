using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using SkiaSharp;

namespace MinorShift.Emuera.UI.Game.Image;

internal sealed class ConstImage : AbstractImage
{
	public ConstImage(string name)
	{ Name = name; RealIsCreated = false; }

	public readonly string Name;
	public SKBitmap RealBitmap;
	public string Filepath;
	public int Width;
	public int Height;
	public bool RealIsCreated;

	internal void CreateFrom(SKBitmap bmp, string filepath, bool useGDI)
	{
		if (RealBitmap != null || !string.IsNullOrEmpty(Filepath))
			throw new Exception();

		try
		{
			RealBitmap = bmp;
			Filepath = filepath;
			Width = RealBitmap.Width;
			Height = RealBitmap.Height;
			lock (AppContents.tempLoadedConstImages)
				AppContents.tempLoadedConstImages.Add(this);
			RealIsCreated = true;
			return;
		}
		catch (Exception e)
		{
			File.WriteAllText("img_err.log", e.ToString());
		}
		return;
	}

	public void Load()
	{
		if (RealBitmap != null || !RealIsCreated)
			return;
		try
		{
			RealBitmap = SKBitmap.Decode(Filepath);
			if (RealBitmap == null)
			{
				return;
			}
			lock (AppContents.tempLoadedConstImages)
				AppContents.tempLoadedConstImages.Add(this);
		}
		catch
		{
			return;
		}
		return;
	}
	//public void Load(bool useGDI)
	//{
	//	if (Loaded)
	//		return;
	//	try
	//	{
	//		Bitmap = new Bitmap(Filepath);
	//		if (useGDI)
	//		{
	//			hBitmap = Bitmap.GetHbitmap();
	//			canvas = Graphics.FromImage(Bitmap);
	//			GDIhDC = canvas.GetHdc();
	//			hDefaultImg = GDI.SelectObject(GDIhDC, hBitmap);
	//		}
	//		Loaded = true;
	//		Enabled = true;
	//	}
	//	catch
	//	{
	//		return;
	//	}
	//	return;
	//}

	public override void Dispose()
	{
		if (RealBitmap == null || !RealIsCreated)
			return;
		if (canvas != null)
		{
			canvas.Dispose();
			canvas = null;
		}
		if (RealBitmap != null)
		{
			RealBitmap.Dispose();
			RealBitmap = null;
		}
	}

	~ConstImage()
	{
		Dispose();
	}


	public override bool IsCreated
	{
		get
		{
			return RealIsCreated;
		}
	}
	public override SKBitmap SKBitmap
	{
		set
		{
			RealBitmap = value;
		}
		get
		{
			Load();
			return RealBitmap;
		}
	}
}
