using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Models;

/// <summary>Puerto de progreso. En Fase 1 es no-op; en Fase 3 lo implementa el hub de SignalR.</summary>
public sealed record ImportProgress(
    Guid JobId,
    int Processed,
    int Created,
    int Errors,
    int Duplicates,
    ImportStatus Status);
