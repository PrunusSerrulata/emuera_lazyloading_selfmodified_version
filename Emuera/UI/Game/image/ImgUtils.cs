using MinorShift.Emuera.Runtime.Utils;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Drawing;
using System.IO;

namespace MinorShift.Emuera.UI.Game.Image;

static class ImgUtils
{
	public static Bitmap LoadImage(string filepath)
	{
		if (!File.Exists(filepath))
		{
			return null;
		}
		
		try
		{
			using var skbmp = SKBitmap.Decode(filepath);
			if (skbmp != null)
			{
				return skbmp.ToBitmap();
			}
			return new Bitmap(filepath);
		}
		catch
		{
			return null;
		}
	}
}
