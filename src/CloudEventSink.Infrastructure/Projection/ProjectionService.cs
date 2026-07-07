using System.Globalization;
using System.Text;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Projection;
using CloudEventSink.Core.Schema;
using CloudEventSink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Projection;

public sealed class ProjectionService : IProjectionService
{
    private readonly ISourceRepository sourceRepository;
    private readonly ISchemaRepository schemaRepository;
    private readonly ISchemaProjectionRepository projectionRepository;
    private readonly IProjectionGenerator generator;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;
    private readonly AppDbContext dbContext;

    public ProjectionService(
        ISourceRepository sourceRepository,
        ISchemaRepository schemaRepository,
        ISchemaProjectionRepository projectionRepository,
        IProjectionGenerator generator,
        IUnitOfWork unitOfWork,
        IClock clock,
        AppDbContext dbContext
    )
    {
        this.sourceRepository = sourceRepository;
        this.schemaRepository = schemaRepository;
        this.projectionRepository = projectionRepository;
        this.generator = generator;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.dbContext = dbContext;
    }

    public async Task<int> RegenerateAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        Source? source = await this
            .sourceRepository.GetByIdAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return 0;
        }

        IReadOnlyList<InferredSchema> schemas = await this
            .schemaRepository.ListBySourceAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);

        int generated = 0;
        foreach (InferredSchema schema in schemas)
        {
            ProjectionSpec spec = await ResolveSpecAsync(source, schema, cancellationToken)
                .ConfigureAwait(false);
            await ApplyAsync(source, schema.EventType, spec, cancellationToken)
                .ConfigureAwait(false);
            generated++;
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return generated;
    }

    public async Task<ProjectionSpec?> GetSpecAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        Source? source = await this
            .sourceRepository.GetByIdAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return null;
        }

        InferredSchema? schema = await FindSchemaAsync(sourceId, eventType, cancellationToken)
            .ConfigureAwait(false);
        if (schema is null)
        {
            return null;
        }

        return await ResolveSpecAsync(source, schema, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveSpecAsync(
        Guid sourceId,
        string eventType,
        ProjectionSpec spec,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(spec);

        Source? source = await this
            .sourceRepository.GetByIdAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return;
        }

        await ApplyAsync(source, eventType, spec, cancellationToken).ConfigureAwait(false);
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetSpecAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        Source? source = await this
            .sourceRepository.GetByIdAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return;
        }

        InferredSchema? schema = await FindSchemaAsync(sourceId, eventType, cancellationToken)
            .ConfigureAwait(false);
        if (schema is null)
        {
            return;
        }

        FieldNode root = FieldNodeSerializer.Deserialize(schema.RootNodeJson);
        ProjectionSpec spec = ProjectionSpecFactory.BuildDefault(source.Slug, eventType, root);
        await ApplyAsync(source, eventType, spec, cancellationToken).ConfigureAwait(false);
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectionSpec> ResolveSpecAsync(
        Source source,
        InferredSchema schema,
        CancellationToken cancellationToken
    )
    {
        FieldNode root = FieldNodeSerializer.Deserialize(schema.RootNodeJson);
        ProjectionSpec fresh = ProjectionSpecFactory.BuildDefault(
            source.Slug,
            schema.EventType,
            root
        );

        SchemaProjection? existing = await this
            .projectionRepository.GetAsync(source.Id, schema.EventType, cancellationToken)
            .ConfigureAwait(false);
        if (existing?.SpecJson is null)
        {
            return fresh;
        }

        return ProjectionSpecMerger.Merge(
            ProjectionSpecSerializer.Deserialize(existing.SpecJson),
            fresh
        );
    }

    private async Task ApplyAsync(
        Source source,
        string eventType,
        ProjectionSpec spec,
        CancellationToken cancellationToken
    )
    {
        ProjectionPlan plan = this.generator.Generate(source.Id, eventType, spec);

        SchemaProjection? existing = await this
            .projectionRepository.GetAsync(source.Id, eventType, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            await this
                .dbContext.Database.ExecuteSqlRawAsync(
                    BuildDropForExisting(existing),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        await this
            .dbContext.Database.ExecuteSqlRawAsync(plan.CreateSql, cancellationToken)
            .ConfigureAwait(false);

        string columnsJson = ProjectionSerializer.SerializeColumns(plan.MainView.Columns);
        string childViewsJson = ProjectionSerializer.SerializeViews(plan.ChildViews);
        string specJson = ProjectionSpecSerializer.Serialize(spec);

        if (existing is null)
        {
            this.projectionRepository.Add(
                new SchemaProjection
                {
                    Id = Guid.NewGuid(),
                    SourceId = source.Id,
                    EventType = eventType,
                    MainViewName = plan.MainView.Name,
                    ColumnsJson = columnsJson,
                    ChildViewsJson = childViewsJson,
                    SpecJson = specJson,
                    GeneratedAtUtc = this.clock.UtcNow,
                }
            );
        }
        else
        {
            existing.MainViewName = plan.MainView.Name;
            existing.ColumnsJson = columnsJson;
            existing.ChildViewsJson = childViewsJson;
            existing.SpecJson = specJson;
            existing.GeneratedAtUtc = this.clock.UtcNow;
        }
    }

    private async Task<InferredSchema?> FindSchemaAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<InferredSchema> schemas = await this
            .schemaRepository.ListBySourceAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        return schemas.FirstOrDefault(schema =>
            string.Equals(schema.EventType, eventType, StringComparison.Ordinal)
        );
    }

    private static string BuildDropForExisting(SchemaProjection existing)
    {
        StringBuilder builder = new StringBuilder();
        foreach (
            ProjectedView child in ProjectionSerializer.DeserializeViews(existing.ChildViewsJson)
        )
        {
            builder.Append(
                CultureInfo.InvariantCulture,
                $"DROP VIEW IF EXISTS \"{child.Name}\" CASCADE;\n"
            );
        }

        builder.Append(
            CultureInfo.InvariantCulture,
            $"DROP VIEW IF EXISTS \"{existing.MainViewName}\" CASCADE;"
        );
        return builder.ToString();
    }
}
