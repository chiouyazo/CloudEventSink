using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext dbContext;

    public DashboardRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Dashboard>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext
            .Dashboards.AsNoTracking()
            .OrderBy(dashboard => dashboard.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext
            .Dashboards.FirstOrDefaultAsync(dashboard => dashboard.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DashboardPanel>> ListPanelsAsync(
        Guid dashboardId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .DashboardPanels.AsNoTracking()
            .Where(panel => panel.DashboardId == dashboardId)
            .OrderBy(panel => panel.Position)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DashboardPanel?> GetPanelAsync(
        Guid panelId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .DashboardPanels.FirstOrDefaultAsync(panel => panel.Id == panelId, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Dashboard dashboard)
    {
        dbContext.Dashboards.Add(dashboard);
    }

    public void Remove(Dashboard dashboard)
    {
        dbContext.Dashboards.Remove(dashboard);
    }

    public void AddPanel(DashboardPanel panel)
    {
        dbContext.DashboardPanels.Add(panel);
    }

    public void RemovePanel(DashboardPanel panel)
    {
        dbContext.DashboardPanels.Remove(panel);
    }
}
