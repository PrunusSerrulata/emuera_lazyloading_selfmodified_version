using MinorShift.Emuera.Runtime.Config;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using MinorShift.Emuera.UI.Game;

namespace MinorShift.Emuera.UI;

internal class FontFactory
{

	static readonly Dictionary<(string fontname, float fontSize, FontStyle fontStyle), SKFont> fontDic = [];

	public static SKFont GetFont(StringStyle stringStyle)
	{
		return GetFont(stringStyle.Fontname, stringStyle.FontStyle);
	}
	public static SKFont GetFont(string requestFontName, FontStyle style, float? fontSize = null)
	{
		/*
		string fontname = requestFontName;
		if (string.IsNullOrEmpty(requestFontName))
			fontname = Config.FontName;
		if (!fontDic.ContainsKey((fontname, Config.FontSize, style)))
		{
			var font = new Font(fontname, Config.FontSize, style, GraphicsUnit.Pixel);
			if (font == null)
			{
				return null;
			}
			else
			{
				fontDic.Add((fontname, Config.FontSize, style), font);
			}

		}
		#region EE_フォントファイル対応
		int fontsize = Config.FontSize;
		Font styledFont;
		foreach (FontFamily ff in GlobalStatic.Pfc.Families)
		{
			if (ff.Name == fontname)
			{
				styledFont = new Font(ff, fontsize, style, GraphicsUnit.Pixel);
				break;
			}
		}
		#endregion
		return fontDic[(fontname, Config.FontSize, style)];
		*/

		string fn = requestFontName;
		if (string.IsNullOrEmpty(requestFontName))
			fn = Config.FontName;
		fontSize ??= Config.FontSize;
		if (!fontDic.ContainsKey((fn, fontSize.Value, style)))
		{
			var font = new SKFont(SKTypeface.FromFamilyName(fn), Config.FontSize);//, style, GraphicsUnit.Pixel);
			if (font != null)
				fontDic.Add((fn, fontSize.Value, style), font);

		}
		Dictionary<FontStyle, SKFont> fontStyleDic = [];
		if (!fontStyleDic.ContainsKey(style))
		{
			int fontsize = Config.FontSize;
			SKFont styledFont;
			try
			{
				#region EE_フォントファイル対応
				foreach (FontFamily ff in GlobalStatic.Pfc.Families)
				{
					if (ff.Name == fn)
					{
						styledFont = new SKFont(SKTypeface.FromFamilyName(ff.Name), fontSize.Value);//, style, GraphicsUnit.Pixel);
						goto foundfont;
					}
				}
				styledFont = new SKFont(SKTypeface.FromFamilyName(fn), fontSize.Value);//, style, GraphicsUnit.Pixel);
			}
			catch
			{
				return null;
			}
		foundfont:
			#endregion
			fontStyleDic.Add(style, styledFont);
		}
		return fontStyleDic[style];
	}

	public static void ClearFont()
	{
		foreach (var font in fontDic)
		{
			font.Value.Dispose();
		}
		fontDic.Clear();
	}
}
