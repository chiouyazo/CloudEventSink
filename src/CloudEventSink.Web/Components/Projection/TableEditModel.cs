using System.Collections.ObjectModel;
using CloudEventSink.Core.Projection;

namespace CloudEventSink.Web.Components.Projection;

public sealed class TableEditModel
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsChild { get; set; }

    public bool ScalarArray { get; set; }

    public IReadOnlyList<string> Path { get; set; } = [];

    public string? ParentKey { get; set; }

    public ArrayMode Mode { get; set; } = ArrayMode.OwnTable;

    public Collection<ColumnEditModel> Columns { get; } = new Collection<ColumnEditModel>();
}
