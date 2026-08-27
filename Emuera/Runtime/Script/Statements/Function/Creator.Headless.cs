using System;

namespace MinorShift.Emuera.GameData.Function;

internal static partial class FunctionMethodCreator
{
    internal static void HeadlessResetKeyToggles()
    {
        if (!Program.HeadlessMode)
            throw new InvalidOperationException("Input reset is available only in headless mode");
        Array.Clear(keytoggle, 0, keytoggle.Length);
    }

    internal static short[] HeadlessReadKeyToggles()
    {
        if (!Program.HeadlessMode)
            throw new InvalidOperationException("Input observation is available only in headless mode");
        return (short[])keytoggle.Clone();
    }
}
