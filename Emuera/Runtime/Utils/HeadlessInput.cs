using System;
using MinorShift.Emuera.GameData.Function;

namespace MinorShift.Emuera.Runtime.Utils;

/// <summary>Device primitives supplied only by the explicitly enabled reference host.</summary>
internal static class HeadlessInput
{
    internal static bool Enabled { get; private set; }
    internal static bool Active { get; private set; }
    internal static Action Pump { get; set; }

    private static void RequireHeadless()
    {
        if (!Program.HeadlessMode)
            throw new InvalidOperationException("Input injection is available only in headless mode");
    }

    internal static void Enable(bool active)
    {
        RequireHeadless();
        Enabled = true;
        Active = active;
    }

    internal static void SetKey(int keyCode, bool down, bool toggle)
    {
        RequireHeadless();
        if (!Enabled || keyCode < 0 || keyCode >= 256)
            throw new ArgumentOutOfRangeException(nameof(keyCode));
        // Exercise the real device-event setters, including their latch side effects.
        if (down) WinInput.SetKeyPressed(keyCode);
        else WinInput.SetKeyReleased(keyCode);
    }

    internal static short GetState(int keyCode)
    {
        RequireHeadless();
        return keyCode >= 0 && keyCode < 256 ? WinInput.GetKeyState(keyCode) : (short)0;
    }

    internal static void PumpEvents()
    {
        RequireHeadless();
        Pump?.Invoke();
    }

    internal static void Reset()
    {
        RequireHeadless();
        Enabled = false;
        Active = false;
        Pump = null;
        WinInput.ResetAllKeys();
        FunctionMethodCreator.HeadlessResetKeyToggles();
    }
}
