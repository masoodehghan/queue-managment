using QueueManagement.Application.DTOs.Queues;

namespace QueueManagement.Application.Common.Interfaces;

public interface IQueueService
{
    Task<QueueResponseDto> CreateQueueAsync(
        int ownerId,
        CreateQueueDto dto,
        CancellationToken cancellationToken);

    Task<List<QueueResponseDto>> GetUserQueuesAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<QueueResponseDto> GetQueueByIdAsync(
        int queueId,
        CancellationToken cancellationToken);

    Task<QueueResponseDto> UpdateQueueAsync(
        int queueId,
        int userId,
        UpdateQueueDto dto,
        CancellationToken cancellationToken);

    Task DeleteQueueAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken);

    Task<QueueItemResponseDto> AddItemToQueueAsync(
        int queueId,
        int? userId,
        AddQueueItemDto dto,
        CancellationToken cancellationToken);

    Task<List<QueueItemResponseDto>> GetQueueItemsAsync(
        int queueId,
        CancellationToken cancellationToken);

    Task<QueueItemResponseDto> UpdateQueueItemAsync(
        int queueId,
        int itemId,
        int userId,
        AddQueueItemDto dto,
        CancellationToken cancellationToken);

    Task RemoveItemFromQueueAsync(
        int queueId,
        int itemId,
        int userId,
        CancellationToken cancellationToken);

    Task<QueueItemResponseDto> ProcessNextItemAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken);

    Task<QueueItemResponseDto> CompleteCurrentItemAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken);

    Task<QueueStatusDto> GetQueueStatusAsync(
        int queueId,
        CancellationToken cancellationToken,
        int? userId = null);

    Task<List<QueueStatusDto>> GetUserQueuesStatusAsync(
        int userId,
        CancellationToken cancellationToken);
}
