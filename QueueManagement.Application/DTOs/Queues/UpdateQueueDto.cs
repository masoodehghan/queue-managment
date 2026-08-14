namespace QueueManagement.Application.DTOs.Queues;

public sealed record UpdateQueueDto(string Name, string? Description = null, int EstimatedTimePerItem = 5);
