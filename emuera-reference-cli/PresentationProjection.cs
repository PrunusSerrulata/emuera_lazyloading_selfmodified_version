using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.UI.Game;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Emuera.ReferenceCli;

internal sealed record PresentationFont(string Family, string Sha256)
{
    internal static PresentationFont? Read(JsonObject request)
    {
        if (request["presentationFont"] is not JsonNode node) return null;
        var value = node.AsObject();
        var family = OracleService.RequiredString(value, "family");
        var path = OracleService.RequiredString(value, "file");
        var expected = OracleService.RequiredString(value, "sha256");
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("presentation font content hash mismatch");
        return new PresentationFont(family, actual);
    }
}

internal static class PresentationProjection
{
    internal static JsonObject Observe(EmueraConsole console, PresentationFont? expected)
    {
        var lines = new JsonArray();
        var providers = new SortedDictionary<string, string?>();
        foreach (var line in console.DisplayLineList)
        {
            var buttons = new JsonArray();
            var textBackgroundEligible = false;
            foreach (var button in line.Buttons)
            {
                var nodes = new JsonArray();
                foreach (var node in button.StrArray)
                {
                    var item = new JsonObject
                    {
                        ["kind"] = node.GetType().Name, ["text"] = node.Text,
                        ["x"] = node.PointX, ["width"] = node.Width, ["widthFloat"] = node.WidthF,
                        ["subpixel"] = node.XsubPixel, ["top"] = node.Top, ["bottom"] = node.Bottom,
                        ["error"] = node.Error,
                    };
                    if (node is ConsoleStyledString text)
                    {
                        textBackgroundEligible |= !string.IsNullOrWhiteSpace(text.Text);
                        var measured = text.HeadlessReadFontProvider();
                        providers[measured.Provider] = measured.Version;
                        item["font"] = Font(text, expected);
                    }
                    nodes.Add(item);
                }
                buttons.Add(new JsonObject
                {
                    ["x"] = button.PointX, ["width"] = button.Width, ["subpixel"] = button.XsubPixel,
                    ["isButton"] = button.IsButton, ["isInteger"] = button.IsInteger,
                    ["input"] = button.IsInteger ? JsonValue.Create(button.Input) : JsonValue.Create(button.Inputs),
                    ["nodes"] = nodes,
                });
            }
            lines.Add(new JsonObject
            {
                ["lineNo"] = line.LineNo, ["logical"] = line.IsLogicalLine,
                ["temporary"] = line.IsTemporary, ["lineEnd"] = line.IsLineEnd,
                ["alignment"] = line.Align.ToString(), ["buttons"] = buttons,
                ["textBackgroundEligible"] = textBackgroundEligible,
            });
        }
        var providerVersions = new JsonObject();
        foreach (var provider in providers) providerVersions[provider.Key] = provider.Value;
        var textBackground = console.TextBackgroundColor is { } color
            ? new JsonObject
            {
                ["red"] = color.R, ["green"] = color.G,
                ["blue"] = color.B, ["alpha"] = color.A,
            }
            : null;
        return new JsonObject
        {
            ["version"] = 1, ["pending"] = console.HeadlessHasPendingDisplay,
            ["animationTimer"] = console.AnimeTimer, ["textBackground"] = textBackground,
            ["provider"] = providers.Count == 1 ? providers.Keys.First() : providers.Count == 0 ? "none-observed" : "mixed",
            ["providerVersions"] = providerVersions,
            ["dotnetVersion"] = Environment.Version.ToString(),
            ["requestedFont"] = Config.FontName, ["fontSize"] = Config.FontSize,
            ["drawingMode"] = Config.TextDrawingMode.ToString(),
            ["drawableWidth"] = Config.DrawableWidth, ["lineHeight"] = Config.LineHeight,
            ["printCLength"] = Config.PrintCLength,
            ["suppliedFontFileSha256"] = expected?.Sha256,
            ["fontByteSource"] = "unverified-installed-source",
            ["fontEvidence"] = "supplied file hash and observed family; not proof of selected font bytes",
            ["lines"] = lines,
        };
    }

    static JsonObject Font(ConsoleStyledString text, PresentationFont? expected)
    {
        var measured = text.HeadlessReadFontProvider();
        var family = measured.Family;
        if (expected is not null && !string.Equals(expected.Family, family, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"presentation font mismatch: expected {expected.Family}, resolved {family}");
        var fallbackRuns = new JsonArray();
        foreach (var run in text.HeadlessReadFontRuns())
        {
            if (expected is not null && !string.Equals(expected.Family, run.Family, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"presentation fallback font mismatch: {run.Family}");
            fallbackRuns.Add(new JsonObject { ["text"] = run.Text, ["resolvedFamily"] = run.Family, ["width"] = run.Width });
        }
        return new JsonObject
        {
            ["requestedFamily"] = text.StringStyle.Fontname, ["resolvedFamily"] = family,
            ["provider"] = measured.Provider, ["providerVersion"] = measured.Version,
            ["size"] = measured.Size, ["style"] = text.StringStyle.FontStyle.ToString(),
            ["fallbackRuns"] = fallbackRuns,
        };
    }
}
