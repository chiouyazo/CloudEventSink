using System.Text.Json;
using CloudEventSink.Core.Enums;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Tests;

public sealed class SchemaInferenceServiceTests
{
    private readonly SchemaInferenceService service = new SchemaInferenceService();

    [Fact]
    public void Derive_FlatObject_AssignsScalarKindsPerField()
    {
        FieldNode root = Derive("""{ "name": "a", "count": 3, "active": true }""");

        Assert.Equal(JsonNodeKind.Object, Assert.Single(root.Kinds));
        Assert.Equal(JsonNodeKind.String, Assert.Single(Child(root, "name").Kinds));
        Assert.Equal(JsonNodeKind.Number, Assert.Single(Child(root, "count").Kinds));
        Assert.Equal(JsonNodeKind.Boolean, Assert.Single(Child(root, "active").Kinds));
    }

    [Fact]
    public void Derive_NestedObject_BuildsChildTree()
    {
        FieldNode root = Derive("""{ "outer": { "inner": "x" } }""");

        FieldNode outer = Child(root, "outer");
        Assert.Equal(JsonNodeKind.Object, Assert.Single(outer.Kinds));
        FieldNode inner = Child(outer, "inner");
        Assert.Equal(JsonNodeKind.String, Assert.Single(inner.Kinds));
    }

    [Fact]
    public void Derive_ArrayOfObjects_MergesElementSchema()
    {
        FieldNode root = Derive(
            """{ "items": [ { "id": "a", "n": 1 }, { "id": "b", "n": 2 } ] }"""
        );

        FieldNode items = Child(root, "items");
        Assert.Equal(JsonNodeKind.Array, Assert.Single(items.Kinds));
        Assert.NotNull(items.Element);
        Assert.Equal(JsonNodeKind.Object, Assert.Single(items.Element!.Kinds));
        Assert.Equal(JsonNodeKind.String, Assert.Single(Child(items.Element!, "id").Kinds));
        Assert.Equal(JsonNodeKind.Number, Assert.Single(Child(items.Element!, "n").Kinds));
    }

    [Fact]
    public void Derive_HeterogeneousArray_CollectsAllElementKinds()
    {
        FieldNode root = Derive("""{ "mixed": [ "text", 5, true ] }""");

        FieldNode mixed = Child(root, "mixed");
        Assert.NotNull(mixed.Element);
        Assert.Contains(JsonNodeKind.String, mixed.Element!.Kinds);
        Assert.Contains(JsonNodeKind.Number, mixed.Element!.Kinds);
        Assert.Contains(JsonNodeKind.Boolean, mixed.Element!.Kinds);
    }

    [Fact]
    public void Merge_FieldMissingInOneEvent_LowersPresenceRatioBelowOne()
    {
        FieldNode first = Derive("""{ "always": 1, "sometimes": 2 }""");
        FieldNode second = Derive("""{ "always": 3 }""");

        FieldNode merged = service.RecomputePresence(service.Merge(first, second));

        Assert.Equal(1.0, Child(merged, "always").PresenceRatio, 3);
        Assert.Equal(0.5, Child(merged, "sometimes").PresenceRatio, 3);
    }

    [Fact]
    public void Merge_NullValueObserved_MarksFieldNullable()
    {
        FieldNode first = Derive("""{ "channel": "stable" }""");
        FieldNode second = Derive("""{ "channel": null }""");

        FieldNode merged = service.Merge(first, second);

        FieldNode channel = Child(merged, "channel");
        Assert.True(channel.Nullable);
        Assert.Contains(JsonNodeKind.String, channel.Kinds);
        Assert.Contains(JsonNodeKind.Null, channel.Kinds);
    }

    [Fact]
    public void Merge_FieldChangesTypeBetweenEvents_RecordsBothKinds()
    {
        FieldNode first = Derive("""{ "value": "text" }""");
        FieldNode second = Derive("""{ "value": 42 }""");

        FieldNode merged = service.Merge(first, second);

        FieldNode value = Child(merged, "value");
        Assert.Contains(JsonNodeKind.String, value.Kinds);
        Assert.Contains(JsonNodeKind.Number, value.Kinds);
    }

    [Fact]
    public void Merge_AccumulatesSampleCountOnRoot()
    {
        FieldNode first = Derive("""{ "a": 1 }""");
        FieldNode second = Derive("""{ "a": 2 }""");
        FieldNode third = Derive("""{ "a": 3 }""");

        FieldNode merged = service.Merge(service.Merge(first, second), third);

        Assert.Equal(3L, merged.SeenCount);
    }

    private FieldNode Derive(string dataJson)
    {
        using JsonDocument document = JsonDocument.Parse(dataJson);
        return service.Derive(document.RootElement);
    }

    private static FieldNode Child(FieldNode node, string name)
    {
        return node.Children.Single(child =>
            string.Equals(child.Name, name, StringComparison.Ordinal)
        );
    }
}
