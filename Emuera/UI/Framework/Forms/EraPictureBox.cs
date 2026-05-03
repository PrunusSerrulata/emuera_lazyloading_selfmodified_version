using System;
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;

namespace MinorShift.Emuera.UI.Framework.Forms
{
	internal sealed class EraPictureBox : SKGLControl
	{
		public static bool UseOpenGL { get; set; } = true;

		public static event Action OnOpenGLFailure;

		public EraPictureBox()
		{
			SetStyle(ControlStyles.Opaque, true);
		}

		public static Control CreateInstance()
		{
			if (UseOpenGL)
			{
				return new EraPictureBox();
			}
			else
			{
				return new EraSKControl();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			try
			{
				base.OnPaint(e);
			}
			catch (NullReferenceException)
			{
				if (UseOpenGL)
				{
					UseOpenGL = false;
					OnOpenGLFailure?.Invoke();
				}
			}
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
