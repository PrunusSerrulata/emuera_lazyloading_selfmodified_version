namespace MinorShift.Emuera;

/// <summary>Access points used only by the deterministic reference oracle.</summary>
static partial class Program
{
	internal static bool HeadlessMode { get; private set; }
	internal static long? HeadlessRandomSeed { get; private set; }
	internal static long HeadlessInstructionLimit { get; private set; }
	internal static System.TimeSpan HeadlessTimeout { get; private set; }

	internal static void ConfigureHeadless(string baseDirectory, bool debugMode, long? randomSeed,
		long instructionLimit, System.TimeSpan timeout)
	{
		HeadlessMode = true;
		HeadlessRandomSeed = randomSeed;
		HeadlessInstructionLimit = instructionLimit;
		HeadlessTimeout = timeout;
		DebugMode = debugMode;
		AnalysisMode = false;
		AnalysisFiles = [];
		SetDirPaths(baseDirectory);
		// Program.Main normally receives this from AssemblyData.  The headless
		// oracle has no AssemblyData instance, so its selected project root is
		// also the working directory used by real file-backed engine features.
		WorkingDir = ExeDir;
	}
}
