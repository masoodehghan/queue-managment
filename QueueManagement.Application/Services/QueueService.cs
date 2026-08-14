using Microsoft.EntityFrameworkCore;
using QueueManagement.Application.Common.Exceptions;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Domain.Entities.Common.Enums;
using QueueManagement.Domain.Entities.Queues;
using QueueManagement.Infrastructure.Data;

namespace QueueManagement.Application.Services;

public class QueueService(AppDbContext context) : IQueueService
{
    public async Task<QueueResponseDto> CreateQueueAsync(int ownerId, CreateQueueDto dto,
        CancellationToken cancellationToken)
    {
        var queue = new Queue
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = ownerId,
            EstimatedTimePerItem = dto.EstimatedTimePerItem,
            Status = QueueStatus.Active
        };

        await context.Queues.AddAsync(queue, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await GetQueueByIdAsync(queue.Id, cancellationToken);
    }

    public async Task<List<QueueResponseDto>> GetUserQueuesAsync(int userId, CancellationToken cancellationToken)
    {
        return await context.Queues
            .Include(q => q.Owner)
            .Include(q => q.Items)
            .Where(q => q.OwnerId == userId && q.Status != QueueStatus.Completed)
            .Select(q => new QueueResponseDto(
                q.Id,
                q.Name,
                q.Description,
                q.Owner.FullName,
                q.Status,
                q.EstimatedTimePerItem,
                q.Items.Count,
                q.Items.Count(i => i.Status == QueueItemStatus.Waiting ||
                                   i.Status == QueueItemStatus.InProgress),
                q.CreatedAt
            ))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<QueueResponseDto> GetQueueByIdAsync(int queueId, CancellationToken cancellationToken)
    {
        var queue = await context.Queues
                        .Include(q => q.Owner)
                        .Include(q => q.Items)
                        .FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        return new QueueResponseDto(
            queue.Id,
            queue.Name,
            queue.Description,
            queue.Owner.FullName,
            queue.Status,
            queue.EstimatedTimePerItem,
            queue.Items.Count,
            queue.Items.Count(i => i.Status == QueueItemStatus.Waiting ||
                                   i.Status == QueueItemStatus.InProgress),
            queue.CreatedAt
        );
    }

    public async Task<QueueResponseDto> UpdateQueueAsync(int queueId, int userId, UpdateQueueDto dto,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues.FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        if (queue.OwnerId != userId)
            throw new UnauthorizedException("You are not authorized to update this queue");

        queue.Name = dto.Name;
        queue.Description = dto.Description;
        queue.EstimatedTimePerItem = dto.EstimatedTimePerItem;
        queue.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await GetQueueByIdAsync(queueId, cancellationToken);
    }

    public async Task DeleteQueueAsync(int queueId, int userId, CancellationToken cancellationToken)
    {
        var queue = await context.Queues.FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        if (queue.OwnerId != userId)
            throw new UnauthorizedException("You are not authorized to delete this queue");

        queue.Status = QueueStatus.Completed;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<QueueItemResponseDto> AddItemToQueueAsync(int queueId, int? userId, AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues
                        .Include(q => q.Items)
                        .FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        if (queue.Status != QueueStatus.Active)
            throw new ValidationException("Queue is not active");

        var maxPosition = queue.Items is {Count: > 0} ? queue.Items.Max(i => i.Position) : 0;

        var queueItem = new QueueItem
        {
            QueueId = queueId,
            UserId = userId,
            ItemName = dto.ItemName,
            Position = maxPosition + 1,
            Status = QueueItemStatus.Waiting
        };

        await context.QueueItems.AddAsync(queueItem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToItemDto(queueItem, queue);
    }

    public async Task<List<QueueItemResponseDto>> GetQueueItemsAsync(int queueId, CancellationToken cancellationToken)
    {
        var queueExists = await context.Queues.AnyAsync(q => q.Id == queueId, cancellationToken);

        if (!queueExists)
            throw new NotFoundException($"Queue with ID {queueId} not found");

        var items = await context.QueueItems
            .Include(qi => qi.Queue)
            .Where(qi => qi.QueueId == queueId &&
                         qi.Status != QueueItemStatus.Completed &&
                         qi.Status != QueueItemStatus.Cancelled)
            .OrderBy(qi => qi.Position)
            .ToListAsync(cancellationToken);

        return [.. items.Select(qi => MapToItemDto(qi, qi.Queue))];
    }

    public async Task<QueueItemResponseDto> UpdateQueueItemAsync(int itemId, AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var item = await context.QueueItems
                       .Include(qi => qi.Queue)
                       .FirstOrDefaultAsync(qi => qi.Id == itemId, cancellationToken)
                   ?? throw new NotFoundException($"Queue item with ID {itemId} not found");

        item.ItemName = dto.ItemName;
        await context.SaveChangesAsync(cancellationToken);

        return MapToItemDto(item, item.Queue);
    }

    public async Task RemoveItemFromQueueAsync(int itemId, CancellationToken cancellationToken)
    {
        var item = await context.QueueItems.FirstOrDefaultAsync(qi => qi.Id == itemId, cancellationToken)
                   ?? throw new NotFoundException($"Queue item with ID {itemId} not found");

        item.Status = QueueItemStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);
        await ReorderQueueAsync(item.QueueId);
    }

    public async Task<QueueItemResponseDto> ProcessNextItemAsync(int queueId, int userId,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues.FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        if (queue.OwnerId != userId)
            throw new UnauthorizedException("Only queue owner can process items");

        var nextItem = await context.QueueItems
                           .Where(qi =>
                               qi.QueueId == queueId &&
                               qi.Status == QueueItemStatus.Waiting)
                           .OrderBy(qi => qi.Position)
                           .FirstOrDefaultAsync(cancellationToken)
                       ?? throw new ValidationException("No waiting items in the queue");

        nextItem.Status = QueueItemStatus.InProgress;
        await context.SaveChangesAsync(cancellationToken);

        return MapToItemDto(nextItem, queue);
    }

    public async Task<QueueItemResponseDto> CompleteCurrentItemAsync(int queueId, int userId,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues.FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        if (queue.OwnerId != userId)
            throw new UnauthorizedException("Only queue owner can complete items");

        var currentItem = await context.QueueItems
                              .FirstOrDefaultAsync(qi =>
                                  qi.QueueId == queueId &&
                                  qi.Status == QueueItemStatus.InProgress, cancellationToken)
                          ?? throw new ValidationException("No item is currently in progress");

        currentItem.Status = QueueItemStatus.Completed;
        currentItem.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await ReorderQueueAsync(queueId);

        return MapToItemDto(currentItem, queue);
    }


    public async Task<QueueStatusDto> GetQueueStatusAsync(int queueId, CancellationToken cancellationToken,
        int? userId = null)
    {
        var queue = await context.Queues
                        .Include(q => q.Items)
                        .FirstOrDefaultAsync(q => q.Id == queueId, cancellationToken)
                    ?? throw new NotFoundException($"Queue with ID {queueId} not found");

        var waitingItems = queue.Items
            .Where(i => i.Status == QueueItemStatus.Waiting ||
                        i.Status == QueueItemStatus.InProgress)
            .OrderBy(i => i.Position)
            .ToList();

        var status = new QueueStatusDto(
            queue.Id,
            queue.Name,
            waitingItems.Count
        );

        if (userId.HasValue)
        {
            var userItem = waitingItems.FirstOrDefault(i => i.UserId == userId.Value);
            if (userItem != null)
                status = status with
                {
                    YourPosition = userItem.Position,
                    PeopleAhead = userItem.Position - 1,
                    EstimatedWaitingMinutes = (userItem.Position - 1) * queue.EstimatedTimePerItem,
                    Status = userItem.Status
                };
        }

        return status;
    }

    public async Task<List<QueueStatusDto>> GetUserQueuesStatusAsync(int userId, CancellationToken cancellationToken)
    {
        var userItems = await context.QueueItems
            .Include(qi => qi.Queue)
            .Where(qi => qi.UserId == userId &&
                         (qi.Status == QueueItemStatus.Waiting ||
                          qi.Status == QueueItemStatus.InProgress))
            .ToListAsync(cancellationToken);

        var statuses = new List<QueueStatusDto>();
        foreach (var item in userItems)
            statuses.Add(await GetQueueStatusAsync(item.QueueId, cancellationToken, userId));

        return statuses;
    }

    private async Task ReorderQueueAsync(int queueId)
    {
        var items = await context.QueueItems
            .Where(qi => qi.QueueId == queueId && qi.Status == QueueItemStatus.Waiting)
            .OrderBy(qi => qi.Position)
            .ToListAsync();

        for (var i = 0; i < items.Count; i++)
            items[i].Position = i + 1;

        await context.SaveChangesAsync();
    }

    private QueueItemResponseDto MapToItemDto(QueueItem item, Queue queue)
    {
        return new QueueItemResponseDto(
            item.Id,
            item.QueueId,
            queue.Name,
            item.ItemName,
            item.Position,
            item.Status,
            (item.Position - 1) * queue.EstimatedTimePerItem,
            item.Position - 1,
            item.JoinedAt
        );
    }
}