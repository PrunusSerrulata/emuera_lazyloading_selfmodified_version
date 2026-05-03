using System;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;

namespace MinorShift.Emuera.UI.Framework.Forms
{
	internal sealed class EraPictureBox : SKGLControl
	{
		public static bool UseOpenGL { get; set; } = true;
		public static event Action OpenGLFailed;
		internal static int failureCount = 0;
		private const int MaxFailures = 3;

		public static string RenderingBackend => UseOpenGL ? "SkiaSharp (OpenGL)" : "SkiaSharp (CPU)";

		public EraPictureBox()
		{
			SetStyle(ControlStyles.Opaque, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			try
			{
				base.OnPaint(e);
				failureCount = 0;
			}
			catch (Exception)
			{
				failureCount++;
				if (failureCount >= MaxFailures && UseOpenGL)
				{
					UseOpenGL = false;
					OpenGLFailed?.Invoke();
				}
			}
		}

		public static Control CreateInstance()
		{
			if (UseOpenGL)
				return new EraPictureBox();
			return new EraSKControl();
		}
	}

	internal sealed class EraSKControl : SKControl
	{
		public EraSKControl()
		{
			SetStyle(ControlStyles.Opaque, true);
		}
	}
}
