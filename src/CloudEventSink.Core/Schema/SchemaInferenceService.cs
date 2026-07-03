using System.Text.Json;
using CloudEventSink.Core.Enums;

namespace CloudEventSink.Core.Schema;

public sealed class SchemaInferenceService : ISchemaInferenceService
{
    public FieldNode Derive(JsonElement data)
    {
        return DeriveNode(string.Empty, data);
    }

    public FieldNode Merge(FieldNode existing, FieldNode incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        return MergeNodes(existing, incoming);
    }

    public FieldNode RecomputePresence(FieldNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return WithPresence(root, 1.0);
    }

    private static FieldNode DeriveNode(string name, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return DeriveObject(name, element);
            case JsonValueKind.Array:
                return DeriveArray(name, element);
            case JsonValueKind.String:
                return Scalar(name, JsonNodeKind.String);
            case JsonValueKind.Number:
                return Scalar(name, JsonNodeKind.Number);
            case JsonValueKind.True:
            case JsonValueKind.False:
                return Scalar(name, JsonNodeKind.Boolean);
            default:
                return new FieldNode
                {
                    Name = name,
                    Kinds = [JsonNodeKind.Null],
                    Nullable = true,
                    SeenCount = 1L,
                };
        }
    }

    private static FieldNode DeriveObject(string name, JsonElement element)
    {
        List<FieldNode> children = new List<FieldNode>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            children.Add(DeriveNode(property.Name, property.Value));
        }

        return new FieldNode
        {
            Name = name,
            Kinds = [JsonNodeKind.Object],
            SeenCount = 1L,
            Children = children,
        };
    }

    private static FieldNode DeriveArray(string name, JsonElement element)
    {
        FieldNode? merged = null;
        foreach (JsonElement item in element.EnumerateArray())
        {
            FieldNode derived = DeriveNode(string.Empty, item);
            merged = merged is null ? derived : MergeNodes(merged, derived);
        }

        return new FieldNode
        {
            Name = name,
            Kinds = [JsonNodeKind.Array],
            SeenCount = 1L,
            Element = merged,
        };
    }

    private static FieldNode Scalar(string name, JsonNodeKind kind)
    {
        return new FieldNode
        {
            Name = name,
            Kinds = [kind],
            SeenCount = 1L,
        };
    }

    private static FieldNode MergeNodes(FieldNode existing, FieldNode incoming)
    {
        HashSet<JsonNodeKind> kinds = new HashSet<JsonNodeKind>(existing.Kinds);
        kinds.UnionWith(incoming.Kinds);

        return new FieldNode
        {
            Name = existing.Name,
            Kinds = SortKinds(kinds),
            Nullable = existing.Nullable || incoming.Nullable,
            SeenCount = existing.SeenCount + incoming.SeenCount,
            Children = MergeChildren(existing.Children, incoming.Children),
            Element = MergeElement(existing.Element, incoming.Element),
        };
    }

    private static List<FieldNode> MergeChildren(
        IReadOnlyList<FieldNode> existing,
        IReadOnlyList<FieldNode> incoming
    )
    {
        List<FieldNode> ordered = new List<FieldNode>();
        Dictionary<string, int> indexByName = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (FieldNode child in existing)
        {
            indexByName[child.Name] = ordered.Count;
            ordered.Add(child);
        }

        foreach (FieldNode child in incoming)
        {
            if (indexByName.TryGetValue(child.Name, out int existingIndex))
            {
                ordered[existingIndex] = MergeNodes(ordered[existingIndex], child);
            }
            else
            {
                indexByName[child.Name] = ordered.Count;
                ordered.Add(child);
            }
        }

        return ordered;
    }

    private static FieldNode? MergeElement(FieldNode? existing, FieldNode? incoming)
    {
        if (existing is not null && incoming is not null)
        {
            return MergeNodes(existing, incoming);
        }

        return existing ?? incoming;
    }

    private static FieldNode WithPresence(FieldNode node, double ratio)
    {
        List<FieldNode> children = new List<FieldNode>();
        foreach (FieldNode child in node.Children)
        {
            double childRatio =
                node.SeenCount == 0L ? 0.0 : child.SeenCount / (double)node.SeenCount;
            children.Add(WithPresence(child, childRatio));
        }

        FieldNode? element = node.Element is null ? null : WithPresence(node.Element, 1.0);

        return node with
        {
            PresenceRatio = ratio,
            Children = children,
            Element = element,
        };
    }

    private static List<JsonNodeKind> SortKinds(HashSet<JsonNodeKind> kinds)
    {
        List<JsonNodeKind> ordered = new List<JsonNodeKind>(kinds);
        ordered.Sort();
        return ordered;
    }
}
