using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using System.Globalization;
using System.Text.Json.Nodes;

namespace Emuera.ReferenceCli;

internal sealed class ReferenceHost : IDisposable
{
    EmueraConsole? console;
    string? gameDirectory;
    long? randomSeed;
    readonly HeadlessInputTrace inputTrace = new();
    PresentationFont? presentationFont;

    internal EmueraConsole? ConsoleOrNull => console;
    internal bool IsLoaded => console?.HeadlessProcess is not null;

    internal async Task<JsonNode> Load(JsonObject request)
    {
        var limits = ReadLimits(request);
        var requestedFont = PresentationFont.Read(request);
        Reset();
        presentationFont = requestedFont;
        var directory = Path.GetFullPath(OracleService.RequiredString(request, "gameDir"));
        if (!Directory.Exists(Path.Combine(directory, "csv")) || !Directory.Exists(Path.Combine(directory, "erb")))
            throw new DirectoryNotFoundException("gameDir must contain csv and erb directories");
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        randomSeed = request["seed"]?.GetValue<long>();
        MinorShift.Emuera.Program.ConfigureHeadless(
            directory,
            request["debug"]?.GetValue<bool>() ?? false,
            randomSeed, limits.Instructions, limits.Timeout);
        ConfigData.HeadlessLoad();
        JSONConfig.HeadlessLoad();
        Lang.LoadLanguageFiles();
        Lang.SetLanguage();
        // Construct the regular Emuera backend without creating a native
        // WinForms handle. Creating a hidden Form still blocks under Wine when
        // no interactive desktop is available.
        console = EmueraConsole.CreateHeadless();
        console.noOutputLog = true;
        await console.Initialize();
        gameDirectory = directory;
        return Snapshot(ReadWatches(request), request["observePresentation"]?.GetValue<bool>() == true);
    }

    internal JsonNode LoadSave(JsonObject request)
    {
        RequireLoaded();
        ConfigureLimits(request);
        var path = Path.GetFullPath(OracleService.RequiredString(request, "savePath"));
        console!.HeadlessProcess.HeadlessLoadSave(path);
        randomSeed = null;
        console.HeadlessClearDisplay();
        // Restored system state must run directly, not feed the previous title's input request.
        console.HeadlessResume(null!);
        return Snapshot(ReadWatches(request), request["observePresentation"]?.GetValue<bool>() == true);
    }

    internal JsonNode Execute(JsonObject request)
    {
        RequireLoaded();
        var statement = OracleService.RequiredString(request, "statement");
        var watches = ReadWatches(request).ToArray();
        var observePresentation = request["observePresentation"]?.GetValue<bool>() == true;
        var trace = HeadlessInputTrace.Read(request);
        ConfigureLimits(request);
        inputTrace.Apply(trace);
        console!.HeadlessProcess.HeadlessExecuteLine(statement);
        return Snapshot(watches, observePresentation);
    }

    internal JsonNode AnalyzeProject()
    {
        RequireLoaded();
        return JsonProjection.Project(console!.HeadlessProcess.LabelDictionary);
    }

    internal JsonNode Run(JsonObject request)
    {
        RequireLoaded();
        var entry = request["entry"]?.GetValue<string>();
        var arguments = request["arguments"]?.GetValue<string>() ?? string.Empty;
        var inputs = request["inputs"]?.AsArray().Select(input => input?.ToString() ?? string.Empty).ToArray() ?? [];
        var uiInputs = ReadUiInputs(request);
        var watches = ReadWatches(request).ToArray();
        var observePresentation = request["observePresentation"]?.GetValue<bool>() == true;
        var trace = HeadlessInputTrace.Read(request);
        ConfigureLimits(request);
        inputTrace.Apply(trace);
        if (entry is not null)
        {
            console!.HeadlessProcess.HeadlessPrepareCall(entry, arguments);
            console!.HeadlessResume(null!);
        }
        if (inputs.Length > 0)
        {
            foreach (var input in inputs)
            {
                if (!console!.IsWaitInputState || console.HeadlessProcess.HeadlessRunCompleted) break;
                console.HeadlessResume(input);
            }
        }
        if (uiInputs.Length > 0)
        {
            foreach (var input in uiInputs)
            {
                if (!console!.IsWaitInputState || console.HeadlessProcess.HeadlessRunCompleted) break;
                var text = input.Text;
                var changedByMouse = input.ChangedByMouse;
                // PressEnterKey also touches WinForms-only macro and refresh state. Reproduce
                // its authoritative OneInput normalization here, then enter the unchanged
                // backend path used by other headless inputs.
                if (console.HeadlessInputRequest.OneInput &&
                    (!Config.AllowLongInputByMouse || !changedByMouse) && text.Length > 1)
                    text = text.Remove(1);
                console.HeadlessResume(text);
            }
        }
        return Snapshot(watches, observePresentation);
    }

    static (string Text, bool ChangedByMouse)[] ReadUiInputs(JsonObject request)
    {
        if (request["uiInputs"] is null) return [];
        return request["uiInputs"]!.AsArray().Select(value =>
        {
            var item = value?.AsObject() ?? throw new ArgumentException("ui input must be an object");
            return (OracleService.RequiredString(item, "text"),
                item["changedByMouse"]?.GetValue<bool>() ?? false);
        }).ToArray();
    }

    internal JsonNode Observe(JsonObject request)
    {
        RequireLoaded();
        // Observers must not evaluate watches: GETKEYTRIGGERED and RAND have side effects.
        return Snapshot([], request["observePresentation"]?.GetValue<bool>() ?? true);
    }

    internal JsonNode InjectInput(JsonObject request)
    {
        RequireLoaded();
        if (request["inputTrace"] is null) throw new ArgumentException("inputTrace is required");
        var observePresentation = request["observePresentation"]?.GetValue<bool>() == true;
        var trace = HeadlessInputTrace.Read(request);
        inputTrace.Apply(trace);
        return Snapshot([], observePresentation);
    }

    internal void RequireLoaded()
    {
        if (!IsLoaded) throw new InvalidOperationException("operation requires a loaded game; call 'load' first");
    }

    void ConfigureLimits(JsonObject request)
    {
        var limits = ReadLimits(request);
        console!.HeadlessProcess.ConfigureHeadlessLimits(limits.Instructions, limits.Timeout);
    }

    static (long Instructions, TimeSpan Timeout) ReadLimits(JsonObject request)
    {
        // Real projects can execute several million dispatches between visible waits. Keep the
        // default bounded while allowing the oracle to reach the next user-observable state.
        var instructions = request["instructionLimit"]?.GetValue<long>() ?? 10_000_000;
        var timeoutMs = request["timeoutMs"]?.GetValue<int>() ?? 20_000;
        if (instructions <= 0) throw new ArgumentOutOfRangeException("instructionLimit", "must be positive");
        if (timeoutMs <= 0) throw new ArgumentOutOfRangeException("timeoutMs", "must be positive");
        return (instructions, TimeSpan.FromMilliseconds(timeoutMs));
    }

    JsonNode Snapshot(IEnumerable<string> watches, bool observePresentation = false)
    {
        var output = new JsonArray();
        if (console is not null)
            foreach (var line in console.DisplayLineList) output.Add(line.ToString());
        var result = new JsonObject
        {
            ["gameDir"] = gameDirectory,
            ["state"] = console?.HeadlessState.ToString(),
            ["termination"] = Termination(),
            ["output"] = output,
            ["instructionCount"] = console?.HeadlessProcess?.HeadlessInstructionCount ?? 0,
            ["position"] = JsonProjection.Graph(console?.HeadlessProcess?.GetRunningPosition()),
            ["randomSeed"] = randomSeed,
            ["randomAlgorithm"] = JSONConfig.Data.UseNewRandom ? "dotnet" : "sfmt19937",
        };
        if (console?.HeadlessProcess?.HeadlessRunCompleted != true && console?.HeadlessInputRequest is { } input)
            result["inputRequest"] = JsonProjection.Graph(input, 3);
        var watchValues = new JsonObject();
        foreach (var expression in watches)
        {
            try { watchValues[expression] = Evaluate(expression); }
            catch (Exception error) { watchValues[expression] = new JsonObject { ["error"] = error.Message }; }
        }
        result["watches"] = watchValues;
        if (observePresentation) result["presentation"] = PresentationProjection.Observe(console!, presentationFont);
        if (HeadlessInput.Enabled) result["primitiveInput"] = inputTrace.Observe();
        return result;
    }

    JsonNode? Evaluate(string source)
    {
        var words = LexicalAnalyzer.Analyse(new CharStream(source), LexEndWith.EoL, LexAnalyzeFlag.None);
        var expression = ExpressionParser.ReduceExpressionTerm(words, TermEndWith.EoL)
            ?? throw new CodeEE("watch expression is empty");
        return JsonProjection.Value(expression);
    }

    string Termination()
    {
        var limit = console?.HeadlessProcess?.HeadlessLimitReason;
        if (limit is not null) return limit;
        if (console?.HeadlessProcess?.HeadlessRunCompleted == true) return "completed";
        return console?.HeadlessState switch
        {
            ConsoleState.WaitInput or ConsoleState.WaitInputNoFocus => "waitingInput",
            ConsoleState.Quit => "quit",
            ConsoleState.Error => "error",
            ConsoleState.Running => "running",
            _ => console?.HeadlessState.ToString() ?? "notLoaded",
        };
    }

    static IEnumerable<string> ReadWatches(JsonObject request) =>
        request["watch"] is null ? [] : request["watch"]!.AsArray().Select(item => item!.GetValue<string>());

    internal void Reset()
    {
        inputTrace.Reset();
        presentationFont = null;
        console?.Dispose();
        console = null;
        gameDirectory = null;
        randomSeed = null;
        GlobalStatic.Reset();
        ParserMediator.Initialize(null!);
        ParserMediator.RenameDic.Clear();
        ParserMediator.HeadlessDrainWarnings();
    }

    public void Dispose() => Reset();
}
