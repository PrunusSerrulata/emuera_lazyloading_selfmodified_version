using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Config.JSON;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace MinorShift.Emuera.UI.Game;

public enum DisplayMode
{
	Relative,
	Absolute,//EM+EE互換
	AbsoluteLeftBottom,
	AbsoluteLeftTop
}

public struct TextsWithFont
{
	public string Text;
	public SKFont Font;
	public float Width;
	public float offsetY;
}

/// <summary>
/// 装飾付文字列。stringとStringStyleからなる。
/// </summary>
internal sealed class ConsoleStyledString : AConsoleColoredPart
{
	private ConsoleStyledString() { }
	public ConsoleStyledString(string str, StringStyle style)
	{
		//if ((StaticConfig.TextDrawingMode != TextDrawingMode.GRAPHICS) && (str.IndexOf('\t') >= 0))
		//    str = str.Replace("\t", "");
		Text = str;
		StringStyle = style;
		Font = FontFactory.GetFont(style.Fontname, style.FontStyle);
		if (Font == null)
		{
			Error = true;
			return;
		}
		if (!Font.ContainsGlyphs(Text))
		{
			_fallbackFont = FontFactory.GetFont(Config.DefaultFont.Typeface.FamilyName, style.FontStyle);
			var isFallbackFont = true;
			var builder = new StringBuilder();
			_texts = [];
			foreach (var @char in Text)
			{
				if ((!isFallbackFont && Font.ContainsGlyph(@char)) ||
					(isFallbackFont && !Font.ContainsGlyph(@char)))
				{
					_texts.Add(CreateTextWithFont(isFallbackFont, builder.ToString()));
					builder.Clear();
					
					isFallbackFont = !isFallbackFont;
				}

				builder.Append(@char);
			}
			if (builder.Length > 0)
			{
				_texts.Add(CreateTextWithFont(isFallbackFont, builder.ToString()));
			}
		}
		Color = style.Color;
		ButtonColor = style.ButtonColor;
		colorChanged = style.ColorChanged;
		if (!colorChanged && Color != Config.ForeColor)
			colorChanged = true;
		PointX = -1;
		Width = -1;

		TextsWithFont CreateTextWithFont(bool isFallbackFont, string t)
		{
			var textsWithFont = new TextsWithFont()
			{
				Text = t
			};
			if (isFallbackFont)
			{
				textsWithFont.Font = Font;
			}
			else
			{
				textsWithFont.Font = _fallbackFont;
			}
			using var paint = new SKPaint();
			paint.Typeface = textsWithFont.Font.Typeface;
			paint.TextSize = textsWithFont.Font.Size;
			textsWithFont.Width = paint.MeasureText(textsWithFont.Text);
			return textsWithFont;
		}
	}
	public SKFont Font { get; private set; }
	SKFont _fallbackFont;
	List<TextsWithFont> _texts;//フォントフォールバック用

	public StringStyle StringStyle { get; private set; }
	public override bool CanDivide
	{
		get { return true; }
	}
	//単一のボタンフラグ
	//public bool IsButton { get; set; }
	//indexの文字数の前方文字列とindex以降の後方文字列に分割
	public ConsoleStyledString DivideAt(int index, StringMeasure sm)
	{
		//if ((index <= 0)||(index > Text.Length)||this.Error)
		//	return null;
		ConsoleStyledString ret = DivideAt(index);
		if (ret == null)
			return null;
		SetWidth(sm, XsubPixel);
		ret.SetWidth(sm, XsubPixel);
		return ret;
	}
	public ConsoleStyledString DivideAt(int index)
	{
		if (index <= 0 || index > Text.Length || Error)
			return null;
		string str = Text[index..];
		Text = Text[..index];
		ConsoleStyledString ret = new()
		{
			Font = Font,
			Text = str,
			Color = Color,
			ButtonColor = ButtonColor,
			colorChanged = colorChanged,
			StringStyle = StringStyle,
			XsubPixel = XsubPixel
		};
		return ret;
	}

	public override void SetWidth(StringMeasure sm, float subPixel)
	{
		if (Error)
		{
			Width = 0;
			return;
		}
		if (_texts == null)
		{
			Width = StringMeasure.GetDisplayLength(Text, Font);
		}
		else
		{
			var offsetX = 0.0f;
			foreach (var text in _texts)
			{
				offsetX += text.Width;
			}
			Width = (int)offsetX;
		}
		XsubPixel = subPixel;
		Size = new SKSize(Width, Config.LineHeight);

		#region EmuEra-Rikaichan
		if (!rikaichaned && Config.RikaiEnabled)
		{
			rikaichaned = true;
			int len = Text.Length;
			Ends = new int[len];
			for (int i = 0; i < len; i++)
			{
				string temp = Text.Substring(0, i + 1);
				Ends[i] = StringMeasure.GetDisplayLength(temp, Font);
			}

		}
		#endregion
	}

	public override void DrawTo(SKCanvas graph, SKPoint origin, bool isSelecting, bool isFocus, bool isBackLog, TextDrawingMode mode, bool isButton = false)
	{
		if (Error)
			return;
		var color = Color;
		SKColor? backcolor = null;
		if (isFocus)
		{
			if (JSONConfig.Data.UseButtonFocusBackgroundColor)
			{
				if (!(Color.Yellow.R == color.R &&
					Color.Yellow.G == color.G &&
					Color.Yellow.B == color.B) &&
					!string.IsNullOrWhiteSpace(Text))
				{
					backcolor = SKColors.Gray;
				}
			}
			color = ButtonColor;
		}
		else if (isBackLog && !colorChanged)
		{
			color = Config.LogColor;
		}

		#region EM_私家版_描画拡張
		var paint = new SKPaint
		{
			Color = color.ToSKColor(),
			IsAntialias = false,
			TextAlign = SKTextAlign.Left
		};

		var point = new SKPoint(PointX, origin.Y);
		if (origin.X == -1)//旧来の位置決め方式
		{
			point.X = PointX;
		}
		Point = point;

		if (backcolor.HasValue)
		{
			var size = new SKSize(Width, Font.Size);
			graph.DrawRect(SKRect.Create(point, size), new SKPaint() { Color = backcolor.Value });
		}

		if (_texts == null)
		{
			point.Offset(0, Math.Abs(Font.Metrics.Top));
			graph.DrawText(Text, point.X, point.Y, Font, paint);
		}
		else
		{
			foreach (var text in _texts)
			{
				var offsetPoint = point with { Y = point.Y + Math.Abs(text.Font.Metrics.Top) };
				graph.DrawText(text.Text, offsetPoint.X, offsetPoint.Y, text.Font, paint);

				point.Offset(text.Width, 0);
			}
		}

		#endregion
	}

	//Bitmap Cache
	public void DrawToBitmap(SKCanvas graph, bool isSelecting, bool isBackLog, TextDrawingMode mode, int xOffset)
	{
		if (Error)
			return;
		Color color = Color;
		if (isSelecting)
			color = ButtonColor;
		else if (isBackLog && !colorChanged)
			color = Config.LogColor;

		#region EM_私家版_描画拡張
		/*
		if (mode == TextDrawingMode.GRAPHICS)
			graph.DrawString(Text, Font, new SolidBrush(color), new Point(xOffset, 0));
		else
			// TextRenderer.DrawText(graph, Text, Font, new Point(PointX, pointY), color, TextFormatFlags.NoPrefix);
			TextRenderer.DrawText(graph, Text.AsSpan(), Font, new Point(xOffset, 0), color, TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
		*/
		var bitmapPaint = new SKPaint { TextAlign = SKTextAlign.Left };
		graph.DrawText(AltText, xOffset, 0f, new SKFont(), bitmapPaint);
		#endregion
	}
}
