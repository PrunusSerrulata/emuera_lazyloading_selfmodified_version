using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using MinorShift.Emuera.UI.Game.Image;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using SkiaSharp;

namespace MinorShift.Emuera.GameData.Function
{
    internal static class ResourceManager
    {
        // 资源节点定义
        internal abstract class ResourceNode { }
        internal class ImageNode : ResourceNode
        {
            public string Src;
            public string Param;
        }
        internal class AnimeNode : ResourceNode
        {
            public int Width;
            public int Height;
            public List<ImageNode> Frames = new List<ImageNode>();
        }

        // 缓存数据
        private static Dictionary<string, ResourceNode> _metadataCache = null;
        
        // G_ID 管理 (从 10000000 开始，避免与游戏脚本自带的 G_ID 冲突)
        private static int _nextGid = 10000000;
        private static Dictionary<string, int> _pathToGid = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        // LRU 缓存管理
        private const int MAX_CACHE_SIZE = 200; // 最大缓存数量(可根据需要调整)
        private static Dictionary<string, long> _spriteLru = new Dictionary<string, long>();
        private static Dictionary<string, List<int>> _spriteToGids = new Dictionary<string, List<int>>(); // 记录 Sprite 依赖了哪些 G_ID
        private static Dictionary<int, int> _gidRefCount = new Dictionary<int, int>(); // G_ID 引用计数

        // 1. 初始化二进制索引
        public static bool InitializeIndex()
        {
            if (_metadataCache != null) return true;

            string indexPath = Path.Combine(Program.ExeDir, "resources.idx");
            if (!File.Exists(indexPath)) return false;

            _metadataCache = new Dictionary<string, ResourceNode>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var fs = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                using (var reader = new BinaryReader(fs))
                {
                    byte[] magic = reader.ReadBytes(4);
                    if (System.Text.Encoding.ASCII.GetString(magic) != "RESX") return false;
                    int version = reader.ReadInt32();
                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        string name = ReadStringFast(reader);
                        byte type = reader.ReadByte();

                        if (type == 0)
                        {
                            _metadataCache[name] = new ImageNode { Src = ReadStringFast(reader), Param = ReadStringFast(reader) };
                        }
                        else if (type == 1)
                        {
                            var node = new AnimeNode { Width = reader.ReadInt32(), Height = reader.ReadInt32() };
                            int frameCount = reader.ReadInt32();
                            for (int f = 0; f < frameCount; f++)
                            {
                                node.Frames.Add(new ImageNode { Src = ReadStringFast(reader), Param = ReadStringFast(reader) });
                            }
                            _metadataCache[name] = node;
                        }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        private static string ReadStringFast(BinaryReader r)
        {
            int len = r.ReadInt32();
            if (len == 0) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(r.ReadBytes(len));
        }

        // 2. 核心：加载资源并更新 LRU
        public static bool LoadResource(string resourceName)
        {
            InitializeIndex();

            // 如果已经加载过了，更新 LRU 时间戳并返回 true
            if (AppContents.GetSprite(resourceName) != null && AppContents.GetSprite(resourceName).IsCreated)
            {
                UpdateLru(resourceName);
                return true;
            }

            if (_metadataCache == null || !_metadataCache.TryGetValue(resourceName, out var node))
                return false;

            List<int> usedGids = new List<int>();

            if (node is ImageNode img)
            {
                int gid = GetOrLoadGraphic(img.Src);
                if (gid == 0) return false;
                usedGids.Add(gid);

                var p = ParseParams(img.Param, gid);
                Rectangle rect = new Rectangle(p.X, p.Y, p.Width, p.Height);
                Point pos = new Point(p.XPos, p.YPos);
                Size destSize = new Size(p.DestW, p.DestH);

                AppContents.CreateSpriteG(resourceName, AppContents.GetGraphics(gid), rect, pos, destSize);
            }
            else if (node is AnimeNode anim)
            {
                AppContents.CreateSpriteAnime(resourceName, anim.Width, anim.Height);
                
                foreach (var frame in anim.Frames)
                {
                    int gid = GetOrLoadGraphic(frame.Src);
                    if (gid == 0) continue;
                    usedGids.Add(gid);

                    var p = ParseParams(frame.Param, gid);
                    Rectangle rect = new Rectangle(p.X, p.Y, p.Width, p.Height);
                    Point pos = new Point(p.XPos, p.YPos);
                    
                    ((SpriteAnime)AppContents.GetSprite(resourceName)).AddFrame(AppContents.GetGraphics(gid), rect, pos, p.Delay);
                }
            }

            // 记录依赖关系并更新 LRU
            _spriteToGids[resourceName] = usedGids;
            UpdateLru(resourceName);
            
            // 检查是否需要淘汰缓存
            CheckCacheLimit();

            return true;
        }

        // 3. 获取或加载 Graphic (分配 G_ID)
        private static int GetOrLoadGraphic(string src)
        {
            if (_pathToGid.TryGetValue(src, out int existingGid))
            {
                _gidRefCount[existingGid]++;
                return existingGid;
            }

            string fullPath = Path.Combine(Program.ExeDir, "resources", src);
            if (!File.Exists(fullPath)) return 0;

            int newGid = _nextGid++;
            // ✅ SkiaSharp 直接解码 WebP
            using var skbmp = SKBitmap.Decode(fullPath);
            if (skbmp == null) return 0;

            var g = AppContents.GetGraphics(newGid);
            g.GCreateFromF(skbmp, false);

            if (g.IsCreated)
            {
                _pathToGid[src] = newGid;
                _gidRefCount[newGid] = 1;
                return newGid;
            }
            return 0;
        }

        // 4. 解析参数 (替代 ERB 的 RM_PARSE_PARAMS)
        private struct ParsedParams
        {
            public int X, Y, Width, Height, XPos, YPos, Delay, DestW, DestH;
        }
        private static ParsedParams ParseParams(string paramStr, int gid)
        {
            var g = AppContents.GetGraphics(gid);
            var p = new ParsedParams { Width = g.Width, Height = g.Height, DestW = g.Width, DestH = g.Height };
            
            if (string.IsNullOrEmpty(paramStr)) return p;

            string[] parts = paramStr.Split(',');
            if (parts.Length >= 1) int.TryParse(parts[0], out p.X);
            if (parts.Length >= 2) int.TryParse(parts[1], out p.Y);
            if (parts.Length >= 3) { int.TryParse(parts[2], out p.Width); p.DestW = p.Width; }
            if (parts.Length >= 4) { int.TryParse(parts[3], out p.Height); p.DestH = p.Height; }
            if (parts.Length >= 5) int.TryParse(parts[4], out p.XPos);
            if (parts.Length >= 6) int.TryParse(parts[5], out p.YPos);
            if (parts.Length >= 7) int.TryParse(parts[6], out p.Delay);
            if (parts.Length >= 8) int.TryParse(parts[7], out p.DestW);
            if (parts.Length >= 9) int.TryParse(parts[8], out p.DestH);

            return p;
        }

        // 5. LRU 缓存管理
        private static void UpdateLru(string resourceName)
        {
            _spriteLru[resourceName] = Environment.TickCount64; // 更新为当前毫秒级时间戳
        }

        private static void CheckCacheLimit()
        {
            if (_spriteLru.Count <= MAX_CACHE_SIZE) return;

            // 找出最久未使用的资源
            var oldest = _spriteLru.OrderBy(kvp => kvp.Value).First().Key;
            ReleaseResource(oldest);
        }

        public static void ReleaseResource(string resourceName)
        {
            if (_spriteLru.ContainsKey(resourceName))
            {
                // 1. 释放 Sprite
                AppContents.SpriteDispose(resourceName);
                _spriteLru.Remove(resourceName);

                // 2. 释放关联的 Graphics (考虑引用计数)
                if (_spriteToGids.TryGetValue(resourceName, out var gids))
                {
                    foreach (int gid in gids)
                    {
                        if (_gidRefCount.ContainsKey(gid))
                        {
                            _gidRefCount[gid]--;
                            if (_gidRefCount[gid] <= 0)
                            {
                                AppContents.GetGraphics(gid).GDispose();
                                _gidRefCount.Remove(gid);
                                // 从路径映射中移除
                                var item = _pathToGid.FirstOrDefault(kvp => kvp.Value == gid);
                                if (item.Key != null) _pathToGid.Remove(item.Key);
                            }
                        }
                    }
                    _spriteToGids.Remove(resourceName);
                }
            }
        }

        public static void ReleaseAll()
        {
            var keys = _spriteLru.Keys.ToList();
            foreach (var key in keys)
            {
                ReleaseResource(key);
            }
            _nextGid = 10000000;
            _gidRefCount.Clear();
            _pathToGid.Clear();
            _spriteToGids.Clear();
        }

		// 存在性检测
        public static bool CheckResourceExists(string resourceName)
        {
            InitializeIndex();

            // 1. 先检查是否已经在内存中 (可能由其他原生代码创建)
            var sprite = AppContents.GetSprite(resourceName);
            if (sprite != null && sprite.IsCreated)
                return true;

            // 2. 检查二进制索引字典中是否存在该条目 (O(1) 极速查询)
            if (_metadataCache != null && _metadataCache.ContainsKey(resourceName))
                return true;

            return false;
        }
    }
}