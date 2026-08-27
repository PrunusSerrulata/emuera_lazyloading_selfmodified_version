using System.Text.Json;
using System.Text.Json.Nodes;

namespace Emuera.ReferenceCli;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        // .NET 8's console code-page setters fail for redirected Wine handles.
        // The protocol owns UTF-8 streams and never needs a native console.
        using var input = new StreamReader(Console.OpenStandardInput(), System.Text.Encoding.UTF8);
        using var output = new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false))
        {
            AutoFlush = true,
        };
        using var service = new OracleService();
        string? line;
        while ((line = input.ReadLine()) is not null)
        {
            JsonObject response;
            try
            {
                var request = JsonNode.Parse(line) as JsonObject
                    ?? throw new JsonException("request must be a JSON object");
                response = service.Handle(request).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                response = Response.Error(null, exception, new JsonArray());
            }
            output.WriteLine(response.ToJsonString(JsonOptions.Compact));
        }
        return 0;
    }
}

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
    };
}

internal static class Response
{
    internal const int SchemaVersion = 2;
    internal const string ReferenceCommit = "fc4fb21416768c17256d0e82f997e5f99c9bba91";

    internal static JsonObject Success(JsonNode? id, JsonNode? result, JsonArray diagnostics) => new()
    {
        ["id"] = id?.DeepClone(), ["ok"] = true, ["schemaVersion"] = SchemaVersion,
        ["referenceCommit"] = ReferenceCommit, ["diagnostics"] = diagnostics, ["result"] = result,
    };

    internal static JsonObject Error(JsonNode? id, Exception exception, JsonArray diagnostics) => new()
    {
        ["id"] = id?.DeepClone(), ["ok"] = false, ["schemaVersion"] = SchemaVersion,
        ["referenceCommit"] = ReferenceCommit, ["diagnostics"] = diagnostics,
        ["error"] = new JsonObject
        {
            ["type"] = exception.GetType().FullName,
            ["message"] = exception.Message,
        },
    };
}
