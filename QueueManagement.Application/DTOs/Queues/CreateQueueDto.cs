namespace QueueManagement.Application.DTOs.Queues;

public sealed record CreateQueueDto(string Name, string? Description = null, int EstimatedTimePerItem = 5);
