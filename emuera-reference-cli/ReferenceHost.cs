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

    internal EmueraConsole? ConsoleOrNull => console;
    internal bool IsLoaded => console?.HeadlessProcess is not null;

    internal async Task<JsonNode> Load(JsonObject request)
    {
        var limits = ReadLimits(request);
        Reset();
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
        return Snapshot(ReadWatches(request));
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
        return Snapshot(ReadWatches(request));
    }

    internal JsonNode Execute(JsonObject request)
    {
        RequireLoaded();
        ConfigureLimits(request);
        console!.HeadlessProcess.HeadlessExecuteLine(OracleService.RequiredString(request, "statement"));
        return Snapshot(ReadWatches(request));
    }

    internal JsonNode AnalyzeProject()
    {
        RequireLoaded();
        return JsonProjection.Project(console!.HeadlessProcess.LabelDictionary);
    }

    internal JsonNode Run(JsonObject request)
    {
        RequireLoaded();
        ConfigureLimits(request);
        if (request["entry"] is JsonValue entryNode)
        {
            var entry = entryNode.GetValue<string>();
            var arguments = request["arguments"]?.GetValue<string>();
            console!.HeadlessProcess.HeadlessPrepareCall(entry, arguments ?? string.Empty);
            console!.HeadlessResume(null!);
        }
        if (request["inputs"] is JsonArray inputs)
        {
            foreach (var input in inputs)
            {
                if (!console!.IsWaitInputState || console.HeadlessProcess.HeadlessRunCompleted) break;
                console.HeadlessResume(input?.ToString() ?? string.Empty);
            }
        }
        if (request["uiInputs"] is JsonArray uiInputs)
        {
            foreach (var input in uiInputs)
            {
                if (!console!.IsWaitInputState || console.HeadlessProcess.HeadlessRunCompleted) break;
                var item = input!.AsObject();
                var text = OracleService.RequiredString(item, "text");
                var changedByMouse = item["changedByMouse"]?.GetValue<bool>() ?? false;
                // PressEnterKey also touches WinForms-only macro and refresh state. Reproduce
                // its authoritative OneInput normalization here, then enter the unchanged
                // backend path used by other headless inputs.
                if (console.HeadlessInputRequest.OneInput &&
                    (!Config.AllowLongInputByMouse || !changedByMouse) && text.Length > 1)
                    text = text.Remove(1);
                console.HeadlessResume(text);
            }
        }
        return Snapshot(ReadWatches(request));
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

    JsonNode Snapshot(IEnumerable<string> watches)
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
        request["watch"] is JsonArray array ? array.Select(item => item!.GetValue<string>()) : [];

    internal void Reset()
    {
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
