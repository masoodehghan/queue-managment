using QueueManagement.Domain.Entities.Common.Enums;

namespace QueueManagement.Application.DTOs.Queues;

public sealed record QueueStatusDto(
    int QueueId = 0,
    string QueueName = "",
    int TotalInQueue = 0,
    int YourPosition = 0,
    int PeopleAhead = 0,
    int EstimatedWaitingMinutes = 0,
    QueueItemStatus Status = QueueItemStatus.Waiting);
