using System.Collections.ObjectModel;
using CloudEventSink.Core.Projection;

namespace CloudEventSink.Web.Components.Projection;

public sealed class ProjectionEditModel
{
    public Collection<TableEditModel> Tables { get; } = new Collection<TableEditModel>();

    public static ProjectionEditModel FromSpec(ProjectionSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ProjectionEditModel model = new ProjectionEditModel();
        foreach (TableSpec table in spec.Tables)
        {
            TableEditModel editable = new TableEditModel
            {
                Key = table.Key,
                Name = table.Name,
                IsChild = table.IsChild,
                ScalarArray = table.ScalarArray,
                Path = table.Path,
                ParentKey = table.ParentKey,
                Mode = table.Mode,
            };
            foreach (ColumnSpec column in table.Columns)
            {
                editable.Columns.Add(
                    new ColumnEditModel
                    {
                        Name = column.Name,
                        SourcePath = column.SourcePath,
                        SqlType = column.SqlType,
                        Included = column.Included,
                        Role = column.Role,
                    }
                );
            }

            model.Tables.Add(editable);
        }

        return model;
    }

    public ProjectionSpec ToSpec()
    {
        return new ProjectionSpec
        {
            Tables = this
                .Tables.Select(table => new TableSpec
                {
                    Key = table.Key,
                    Name = table.Name.Trim(),
                    IsChild = table.IsChild,
                    ScalarArray = table.ScalarArray,
                    Path = table.Path,
                    ParentKey = table.ParentKey,
                    Mode = table.Mode,
                    Columns = table
                        .Columns.Select(column => new ColumnSpec
                        {
                            Name = column.Name.Trim(),
                            SourcePath = column.SourcePath,
                            SqlType = column.SqlType,
                            Included = column.Included,
                            Role = column.Role,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }
}
