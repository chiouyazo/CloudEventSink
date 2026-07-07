using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Query;

namespace CloudEventSink.Core.Projection;

public static class ProjectionCatalogFactory
{
    public static ProjectionCatalog Create(IReadOnlyList<SchemaProjection> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);

        Dictionary<string, IReadOnlyDictionary<string, string>> views = new Dictionary<
            string,
            IReadOnlyDictionary<string, string>
        >(StringComparer.Ordinal);

        foreach (SchemaProjection projection in projections)
        {
            IReadOnlyList<ProjectedColumn> mainColumns = ProjectionSerializer.DeserializeColumns(
                projection.ColumnsJson
            );
            views[projection.MainViewName] = ToColumnMap(mainColumns);

            IReadOnlyList<ProjectedView> childViews = ProjectionSerializer.DeserializeViews(
                projection.ChildViewsJson
            );
            foreach (ProjectedView child in childViews)
            {
                views[child.Name] = ToColumnMap(child.Columns);
            }
        }

        return new ProjectionCatalog { Views = views };
    }

    private static Dictionary<string, string> ToColumnMap(IReadOnlyList<ProjectedColumn> columns)
    {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ProjectedColumn column in columns)
        {
            map[column.Name] = column.SqlType;
        }

        return map;
    }
}
