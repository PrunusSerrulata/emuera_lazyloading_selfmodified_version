using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.Runtime.Utils;
using System.Text.Json.Nodes;

namespace Emuera.ReferenceCli;

/// <summary>Validated device events, never a replacement implementation of GETKEY.</summary>
internal sealed class HeadlessInputTrace
{
    const int MaximumEvents = 4096;
    readonly Queue<KeyEvent[]> pumps = new();
    long pumpCount;
    long eventCount;

    internal readonly record struct KeyEvent(int KeyCode, bool Down, bool Toggle);
    internal sealed record Prepared(bool Active, KeyEvent[] Immediate, KeyEvent[][]? Pumps);

    internal static Prepared? Read(JsonObject request)
    {
        if (request["inputTrace"] is not JsonNode node) return null;
        var trace = node.AsObject();
        var active = trace["active"]?.GetValue<bool>() ?? HeadlessInput.Active;
        var remaining = MaximumEvents;
        var immediate = ReadEvents(trace["beforeRun"], ref remaining);
        var queued = new List<KeyEvent[]>();
        if (trace["awaitPumps"] is JsonNode pumpNode)
        {
            var array = pumpNode.AsArray();
            if (array.Count > 256) throw new ArgumentException("at most 256 AWAIT pumps are allowed");
            foreach (var batch in array) queued.Add(ReadEvents(batch, ref remaining));
        }
        return new Prepared(active, immediate, trace.ContainsKey("awaitPumps") ? queued.ToArray() : null);
    }

    internal void Apply(Prepared? trace)
    {
        if (trace is null) return;
        // The host validates the complete request before applying this immutable event plan.
        HeadlessInput.Enable(trace.Active);
        if (trace.Pumps is not null)
        {
            pumps.Clear();
            foreach (var batch in trace.Pumps) pumps.Enqueue(batch);
        }
        HeadlessInput.Pump = Pump;
        ApplyEvents(trace.Immediate);
    }

    static KeyEvent[] ReadEvents(JsonNode? node, ref int remaining)
    {
        if (node is null) return [];
        var array = node.AsArray();
        remaining -= array.Count;
        if (remaining < 0) throw new ArgumentException("input trace exceeds 4096 events");
        var events = new List<KeyEvent>();
        foreach (var value in array)
        {
            var item = value?.AsObject() ?? throw new ArgumentException("input event must be an object");
            var code = item["keyCode"]?.GetValue<int>() ?? throw new ArgumentException("keyCode is required");
            if (code < 0 || code > 255) throw new ArgumentOutOfRangeException("keyCode");
            var down = item["down"]?.GetValue<bool>() ?? throw new ArgumentException("down is required");
            var toggle = item["toggle"]?.GetValue<bool>() ?? false;
            events.Add(new KeyEvent(code, down, toggle));
        }
        return events.ToArray();
    }

    void ApplyEvents(IEnumerable<KeyEvent> events)
    {
        foreach (var item in events)
        {
            HeadlessInput.SetKey(item.KeyCode, item.Down, item.Toggle);
            eventCount++;
        }
    }

    void Pump()
    {
        pumpCount++;
        if (pumps.TryDequeue(out var batch)) ApplyEvents(batch);
    }

    internal JsonObject Observe()
    {
        var keys = new JsonArray();
        var toggles = FunctionMethodCreator.HeadlessReadKeyToggles();
        for (var code = 0; code < 256; code++)
        {
            var state = HeadlessInput.GetState(code);
            keys.Add(new JsonObject
            {
                ["keyCode"] = code, ["rawState"] = (int)state,
                ["held"] = state < 0, ["toggle"] = (state & 1) != 0,
                ["evaluatorToggle"] = (int)toggles[code],
                ["latch"] = WinInput.HeadlessPeekLatch(code),
            });
        }
        return new JsonObject
        {
            ["version"] = 1, ["enabled"] = HeadlessInput.Enabled, ["active"] = HeadlessInput.Active,
            ["eventCount"] = eventCount, ["pumpCount"] = pumpCount,
            ["pendingPumps"] = pumps.Count, ["keys"] = keys,
        };
    }

    internal void Reset()
    {
        pumps.Clear();
        pumpCount = 0;
        eventCount = 0;
        if (MinorShift.Emuera.Program.HeadlessMode) HeadlessInput.Reset();
    }
}
