using MinorShift.Emuera.Runtime.Config;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using MinorShift.Emuera.UI.Game;
using System;

namespace MinorShift.Emuera.UI;

internal class FontFactory
{

	static readonly Dictionary<(string fontname, float fontSize, FontStyle font_style), SKFont> fontDic = [];
	static readonly Dictionary<(char, string), SKTypeface> fallbackTypefaceCache = [];

	public static SKFont GetFont(StringStyle stringStyle)
	{
		return GetFont(stringStyle.Fontname, stringStyle.FontStyle);
	}
	public static SKFont GetFont(string requestFontName, FontStyle style, float? fontSize = null)
	{
		string fn = requestFontName;
		if (string.IsNullOrEmpty(requestFontName))
			fn = Config.FontName;
		fontSize ??= Config.FontSize;
		
		// 检查字体缓存
		var key = (fn, fontSize.Value, style);
		if (fontDic.TryGetValue(key, out var cachedFont))
		{
			return cachedFont;
		}
		
		// 创建新字体
		try
		{
			var typeface = CreateTypefaceWithFallback(fn);
			var font = new SKFont(typeface, fontSize.Value)
			{
				Hinting = (SKFontHinting)Config.FontHinting,
				Edging = (SKFontEdging)Config.FontEdging
			};
			fontDic[key] = font;
			return font;
		}
		catch
		{
			return null;
		}
	}

	private static SKTypeface CreateTypefaceWithFallback(string fontName)
	{
		// 首先尝试从系统字体创建
		SKTypeface typeface = SKTypeface.FromFamilyName(fontName);
		if (typeface != null)
			return typeface;
		
		// 然后在自定义字体中查找，支持部分匹配
		foreach (var customTypeface in GlobalStatic.CustomTypefaces)
		{
			if (customTypeface != null && customTypeface.FamilyName.Contains(fontName, System.StringComparison.OrdinalIgnoreCase))
				return customTypeface;
		}
		
		// 最后回退到默认字体
		return SKTypeface.Default;
	}

	public static SKTypeface GetTypefaceWithFallback(string fontName)
	{
		return CreateTypefaceWithFallback(fontName);
	}

	public static SKFont GetFallbackFontForChar(char c, float fontSize, FontStyle style)
	{
		foreach (var customTypeface in GlobalStatic.CustomTypefaces)
		{
			if (customTypeface != null && customTypeface.ContainsGlyph(c))
			{
				return new SKFont(customTypeface, fontSize)
				{
					Hinting = (SKFontHinting)Config.FontHinting,
					Edging = (SKFontEdging)Config.FontEdging
				};
			}
		}

		var matchTypeface = SKFontManager.Default.MatchCharacter(c);
		if (matchTypeface != null)
		{
			return new SKFont(matchTypeface, fontSize)
			{
				Hinting = (SKFontHinting)Config.FontHinting,
				Edging = (SKFontEdging)Config.FontEdging
			};
		}

		return GetFont(Config.FontName, style, fontSize);
	}

	public static SKTypeface GetFallbackTypefaceForChar(char c)
	{
		return GetFallbackTypefaceForChar(c, Config.FontName);
	}

	public static SKTypeface GetFallbackTypefaceForChar(char c, string currentFontName)
	{
		var cacheKey = (c, currentFontName);
		if (fallbackTypefaceCache.TryGetValue(cacheKey, out var cachedTypeface))
		{
			return cachedTypeface;
		}

		foreach (var customTypeface in GlobalStatic.CustomTypefaces)
		{
			if (customTypeface != null && customTypeface.ContainsGlyph(c))
			{
				fallbackTypefaceCache[cacheKey] = customTypeface;
				return customTypeface;
			}
		}

		bool isSerif = !string.IsNullOrEmpty(currentFontName) && (
			currentFontName.Contains("Mincho", StringComparison.OrdinalIgnoreCase) ||
			currentFontName.Contains("明朝", StringComparison.OrdinalIgnoreCase) ||
			currentFontName.Contains("宋体", StringComparison.OrdinalIgnoreCase) ||
			currentFontName.Contains("Sun", StringComparison.OrdinalIgnoreCase) ||
			currentFontName.Contains("Serif", StringComparison.OrdinalIgnoreCase));

		string[] safeFallbackFonts;

		if (isSerif)
		{
			safeFallbackFonts = new string[] {
				"MS Mincho", "MS PMincho",
				"SimSun", "NSimSun", "FangSong",
				"MingLiU", "PMingLiU",
				"MS Gothic", "SimHei"
			};
		}
		else
		{
			safeFallbackFonts = new string[] {
				"MS Gothic", "MS PGothic", "MS UI Gothic",
				"SimHei", "Microsoft YaHei",
				"Meiryo", "Yu Gothic",
				"SimSun"
			};
		}

		foreach (var fontName in safeFallbackFonts)
		{
			var safeTypeface = SKTypeface.FromFamilyName(fontName);
			if (safeTypeface != null && safeTypeface.FamilyName.Equals(fontName, StringComparison.OrdinalIgnoreCase) && safeTypeface.ContainsGlyph(c))
			{
				fallbackTypefaceCache[cacheKey] = safeTypeface;
				return safeTypeface;
			}
		}

		var matchTypeface = SKFontManager.Default.MatchCharacter(c);
		if (matchTypeface != null)
		{
			fallbackTypefaceCache[cacheKey] = matchTypeface;
			return matchTypeface;
		}

		fallbackTypefaceCache[cacheKey] = SKTypeface.Default;
		return SKTypeface.Default;
	}

	public static bool TryGetGlyphFromFallback(string fontName, uint codepoint)
	{
		SKTypeface typeface = CreateTypefaceWithFallback(fontName);
		if (typeface == null)
			typeface = SKTypeface.Default;
		if (typeface.GetGlyph((int)codepoint) != 0)
			return true;
		foreach (var customTypeface in GlobalStatic.CustomTypefaces)
		{
			if (customTypeface != null && customTypeface.GetGlyph((int)codepoint) != 0)
				return true;
		}
		return false;
	}

	public static void ClearFont()
	{
		foreach (var font in fontDic)
		{
			font.Value.Dispose();
		}
		fontDic.Clear();
		fallbackTypefaceCache.Clear();
	}
}
