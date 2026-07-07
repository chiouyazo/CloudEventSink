using System.Text.Json.Nodes;
using CloudEventSink.Core.Query;

namespace CloudEventSink.Core.Tests;

public sealed class QueryResultExporterTests
{
    private static QueryResultSet Sample()
    {
        return new QueryResultSet
        {
            Columns =
            [
                new QueryResultColumn { Name = "name", DataType = "text" },
                new QueryResultColumn { Name = "total", DataType = "numeric" },
            ],
            Rows =
            [
                new JsonNode?[] { JsonValue.Create("Kim"), JsonValue.Create(42) },
                new JsonNode?[] { JsonValue.Create("A, B \"x\""), null },
            ],
        };
    }

    [Fact]
    public void ToCsv_WritesHeaderAndEscapesSpecialCharacters()
    {
        string csv = QueryResultExporter.ToCsv(Sample());

        Assert.Contains("name,total", csv, StringComparison.Ordinal);
        Assert.Contains("Kim,42", csv, StringComparison.Ordinal);
        Assert.Contains("\"A, B \"\"x\"\"\",", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void ToJson_ProducesArrayOfObjects()
    {
        string json = QueryResultExporter.ToJson(Sample());

        JsonNode? parsed = JsonNode.Parse(json);
        JsonArray array = Assert.IsType<JsonArray>(parsed);
        Assert.Equal(2, array.Count);
        Assert.Equal("Kim", array[0]!["name"]!.ToString());
    }
}
