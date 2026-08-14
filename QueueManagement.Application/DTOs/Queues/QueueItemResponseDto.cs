using QueueManagement.Domain.Entities.Common.Enums;

namespace QueueManagement.Application.DTOs.Queues;

public sealed record QueueItemResponseDto(
    int Id,
    int QueueId,
    string QueueName,
    string ItemName,
    int Position,
    QueueItemStatus Status,
    int EstimatedWaitingMinutes,
    int PeopleAhead,
    DateTime JoinedAt);
