using System.Windows.Forms;
using SkiaSharp.Views.Desktop;

namespace MinorShift.Emuera.UI.Framework.Forms
{
	internal sealed class EraPictureBox : SKGLControl
	{
		// 静态属性，用于控制是否使用OpenGL加速
		public static bool UseOpenGL { get; set; } = true;

		public EraPictureBox()
		{
			//背景描画カット
			SetStyle(ControlStyles.Opaque, true);
		}

		// 创建EraPictureBox实例的工厂方法
		public static Control CreateInstance()
		{
			if (UseOpenGL)
			{
				return new EraPictureBox();
			}
			else
			{
				// 使用基于CPU的SKControl作为备选
				return new EraSKControl();
			}
		}

	}

	// SKControl的子类，用于设置ControlStyles
	internal sealed class EraSKControl : SKControl
	{
		public EraSKControl()
		{
			//背景描画カット
			SetStyle(ControlStyles.Opaque, true);
		}
	}
}
