namespace ImportaYa.Application.Dtos;

/// <summary>Evento enviado por SignalR mientras se procesa un trabajo.</summary>
public sealed record ImportProgressEvent(
    Guid JobId,
    int Processed,
    int Created,
    int Errors,
    int Duplicates,
    string Status);
