using QueueManagement.Domain.Entities.Common.Enums;

namespace QueueManagement.Application.DTOs.Queues;

public sealed record QueueResponseDto(
    int Id,
    string Name,
    string? Description,
    string OwnerName,
    QueueStatus Status,
    int EstimatedTimePerItem,
    int TotalItems,
    int WaitingItems,
    DateTime CreatedAt);
