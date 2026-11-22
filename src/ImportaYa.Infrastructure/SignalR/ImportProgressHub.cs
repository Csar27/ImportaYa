using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ImportaYa.Infrastructure.SignalR;

/// <summary>Hub autenticado que agrupa a los clientes por trabajo de importación.</summary>
[Authorize(Policy = "ImportAllowed")]
public sealed class ImportProgressHub : Hub
{
    public static string GroupName(Guid jobId) => $"job:{jobId}";

    public Task JoinJob(Guid jobId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(jobId));

    public Task LeaveJob(Guid jobId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(jobId));
}
