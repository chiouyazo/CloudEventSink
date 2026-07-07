using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface IDashboardRepository
{
    Task<IReadOnlyList<Dashboard>> ListAsync(CancellationToken cancellationToken);

    Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DashboardPanel>> ListPanelsAsync(
        Guid dashboardId,
        CancellationToken cancellationToken
    );

    Task<DashboardPanel?> GetPanelAsync(Guid panelId, CancellationToken cancellationToken);

    void Add(Dashboard dashboard);

    void Remove(Dashboard dashboard);

    void AddPanel(DashboardPanel panel);

    void RemovePanel(DashboardPanel panel);
}
