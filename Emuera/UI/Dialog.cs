using System.Windows.Forms;
static class Dialog
{
	public enum Result
	{
		Yes,
		No
	}
	public static void Show(string text)
	{
		if (MinorShift.Emuera.Program.HeadlessMode)
			return;
		MessageBox.Show(text);
	}
	public static void Show(string title, string text)
	{
		if (MinorShift.Emuera.Program.HeadlessMode)
			return;
		MessageBox.Show(text, title);
	}
	public static bool ShowPrompt(string title, string text)
	{
		if (MinorShift.Emuera.Program.HeadlessMode)
			return false;
		var result = MessageBox.Show(text, title, MessageBoxButtons.YesNo);
		return result switch
		{
			DialogResult.Yes => true,
			_ => false
		};
	}
}
