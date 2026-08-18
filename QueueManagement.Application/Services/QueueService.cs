using Microsoft.EntityFrameworkCore;
using QueueManagement.Application.Common.Exceptions;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Domain.Entities.Common.Enums;
using QueueManagement.Domain.Entities.Queues;
using QueueEntity = QueueManagement.Domain.Entities.Queues.Queue;

namespace QueueManagement.Application.Services;

public sealed class QueueService(
    IApplicationDbContext context,
    TimeProvider timeProvider) : IQueueService
{
    public async Task<QueueResponseDto> CreateQueueAsync(
        int ownerId,
        CreateQueueDto dto,
        CancellationToken cancellationToken)
    {
        var queue = new QueueEntity
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            OwnerId = ownerId,
            EstimatedTimePerItem = dto.EstimatedTimePerItem,
            Status = QueueStatus.Active,
            CreatedAt = UtcNow
        };

        context.Queues.Add(queue);
        await context.SaveChangesAsync(cancellationToken);

        return await GetQueueByIdAsync(queue.Id, cancellationToken);
    }

    public Task<List<QueueResponseDto>> GetUserQueuesAsync(
        int userId,
        CancellationToken cancellationToken) =>
        context.Queues
            .AsNoTracking()
            .Where(queue => queue.OwnerId == userId && queue.Status != QueueStatus.Completed)
            .OrderByDescending(queue => queue.CreatedAt)
            .Select(queue => new QueueResponseDto(
                queue.Id,
                queue.Name,
                queue.Description,
                queue.Owner.FullName,
                queue.Status,
                queue.EstimatedTimePerItem,
                queue.Items.Count,
                queue.Items.Count(item =>
                    item.Status == QueueItemStatus.Waiting ||
                    item.Status == QueueItemStatus.InProgress),
                queue.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<QueueResponseDto> GetQueueByIdAsync(
        int queueId,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues
            .AsNoTracking()
            .Where(queue => queue.Id == queueId)
            .Select(queue => new QueueResponseDto(
                queue.Id,
                queue.Name,
                queue.Description,
                queue.Owner.FullName,
                queue.Status,
                queue.EstimatedTimePerItem,
                queue.Items.Count,
                queue.Items.Count(item =>
                    item.Status == QueueItemStatus.Waiting ||
                    item.Status == QueueItemStatus.InProgress),
                queue.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return queue ?? throw new NotFoundException($"Queue {queueId} was not found.");
    }

    public async Task<QueueResponseDto> UpdateQueueAsync(
        int queueId,
        int userId,
        UpdateQueueDto dto,
        CancellationToken cancellationToken)
    {
        var queue = await GetOwnedQueueAsync(queueId, userId, cancellationToken);

        queue.Name = dto.Name.Trim();
        queue.Description = dto.Description?.Trim();
        queue.EstimatedTimePerItem = dto.EstimatedTimePerItem;
        queue.UpdatedAt = UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await GetQueueByIdAsync(queueId, cancellationToken);
    }

    public async Task DeleteQueueAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken)
    {
        var queue = await GetOwnedQueueAsync(queueId, userId, cancellationToken);

        if (queue.Status == QueueStatus.Completed)
        {
            return;
        }

        queue.Status = QueueStatus.Completed;
        queue.UpdatedAt = UtcNow;

        var activeItems = await context.QueueItems
            .Where(item =>
                item.QueueId == queueId &&
                (item.Status == QueueItemStatus.Waiting ||
                 item.Status == QueueItemStatus.InProgress))
            .ToListAsync(cancellationToken);

        foreach (var item in activeItems)
        {
            item.Status = QueueItemStatus.Cancelled;
            item.UpdatedAt = UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<QueueItemResponseDto> AddItemToQueueAsync(
        int queueId,
        int? userId,
        AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues
            .SingleOrDefaultAsync(queue => queue.Id == queueId, cancellationToken)
            ?? throw new NotFoundException($"Queue {queueId} was not found.");

        EnsureQueueIsActive(queue);

        if (userId is not null)
        {
            var alreadyQueued = await context.QueueItems.AnyAsync(
                item =>
                    item.QueueId == queueId &&
                    item.UserId == userId &&
                    (item.Status == QueueItemStatus.Waiting ||
                     item.Status == QueueItemStatus.InProgress),
                cancellationToken);

            if (alreadyQueued)
            {
                throw new ConflictException("You already have an active item in this queue.");
            }
        }

        var lastPosition = await context.QueueItems
            .Where(item =>
                item.QueueId == queueId &&
                (item.Status == QueueItemStatus.Waiting ||
                 item.Status == QueueItemStatus.InProgress))
            .Select(item => (int?)item.Position)
            .MaxAsync(cancellationToken) ?? 0;

        var item = new QueueItem
        {
            QueueId = queueId,
            UserId = userId,
            ItemName = dto.ItemName.Trim(),
            Position = lastPosition + 1,
            Status = QueueItemStatus.Waiting,
            JoinedAt = UtcNow,
            CreatedAt = UtcNow
        };

        context.QueueItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return MapItem(item, queue.Name, queue.EstimatedTimePerItem);
    }

    public async Task<List<QueueItemResponseDto>> GetQueueItemsAsync(
        int queueId,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues
            .AsNoTracking()
            .Where(queue => queue.Id == queueId)
            .Select(queue => new
            {
                queue.Name,
                queue.EstimatedTimePerItem
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Queue {queueId} was not found.");

        return await context.QueueItems
            .AsNoTracking()
            .Where(item =>
                item.QueueId == queueId &&
                (item.Status == QueueItemStatus.Waiting ||
                 item.Status == QueueItemStatus.InProgress))
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Id)
            .Select(item => new QueueItemResponseDto(
                item.Id,
                item.QueueId,
                queue.Name,
                item.ItemName,
                item.Position,
                item.Status,
                (item.Position > 1 ? item.Position - 1 : 0) * queue.EstimatedTimePerItem,
                item.Position > 1 ? item.Position - 1 : 0,
                item.JoinedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<QueueItemResponseDto> UpdateQueueItemAsync(
        int queueId,
        int itemId,
        int userId,
        AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var item = await context.QueueItems
            .Include(item => item.Queue)
            .SingleOrDefaultAsync(
                item => item.Id == itemId && item.QueueId == queueId,
                cancellationToken)
            ?? throw new NotFoundException($"Queue item {itemId} was not found.");

        EnsureCanManageItem(item, userId);
        EnsureItemIsActive(item);

        item.ItemName = dto.ItemName.Trim();
        item.UpdatedAt = UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapItem(item, item.Queue.Name, item.Queue.EstimatedTimePerItem);
    }

    public async Task RemoveItemFromQueueAsync(
        int queueId,
        int itemId,
        int userId,
        CancellationToken cancellationToken)
    {
        var item = await context.QueueItems
            .Include(item => item.Queue)
            .SingleOrDefaultAsync(
                item => item.Id == itemId && item.QueueId == queueId,
                cancellationToken)
            ?? throw new NotFoundException($"Queue item {itemId} was not found.");

        EnsureCanManageItem(item, userId);
        EnsureItemIsActive(item);

        item.Status = QueueItemStatus.Cancelled;
        item.UpdatedAt = UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await ReorderQueueAsync(queueId, cancellationToken);
    }

    public async Task<QueueItemResponseDto> ProcessNextItemAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken)
    {
        var queue = await GetOwnedQueueAsync(queueId, userId, cancellationToken);
        EnsureQueueIsActive(queue);

        var hasInProgressItem = await context.QueueItems.AnyAsync(
            item =>
                item.QueueId == queueId &&
                item.Status == QueueItemStatus.InProgress,
            cancellationToken);

        if (hasInProgressItem)
        {
            throw new ConflictException(
                "This queue already has an item in progress. Complete it before processing the next item.");
        }

        var nextItem = await context.QueueItems
            .Where(item =>
                item.QueueId == queueId &&
                item.Status == QueueItemStatus.Waiting)
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("There are no waiting items in this queue.");

        nextItem.Status = QueueItemStatus.InProgress;
        nextItem.UpdatedAt = UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapItem(nextItem, queue.Name, queue.EstimatedTimePerItem);
    }

    public async Task<QueueItemResponseDto> CompleteCurrentItemAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken)
    {
        var queue = await GetOwnedQueueAsync(queueId, userId, cancellationToken);

        var currentItem = await context.QueueItems
            .Where(item =>
                item.QueueId == queueId &&
                item.Status == QueueItemStatus.InProgress)
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("There is no item in progress for this queue.");

        currentItem.Status = QueueItemStatus.Completed;
        currentItem.CompletedAt = UtcNow;
        currentItem.UpdatedAt = UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await ReorderQueueAsync(queueId, cancellationToken);

        return MapItem(currentItem, queue.Name, queue.EstimatedTimePerItem);
    }

    public async Task<QueueStatusDto> GetQueueStatusAsync(
        int queueId,
        CancellationToken cancellationToken,
        int? userId = null)
    {
        var queue = await context.Queues
            .AsNoTracking()
            .Where(queue => queue.Id == queueId)
            .Select(queue => new
            {
                queue.Id,
                queue.Name,
                queue.EstimatedTimePerItem
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Queue {queueId} was not found.");

        var activeItems = await context.QueueItems
            .AsNoTracking()
            .Where(item =>
                item.QueueId == queueId &&
                (item.Status == QueueItemStatus.Waiting ||
                 item.Status == QueueItemStatus.InProgress))
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.UserId,
                item.Status
            })
            .ToListAsync(cancellationToken);

        if (userId is null)
        {
            return new QueueStatusDto(
                queue.Id,
                queue.Name,
                activeItems.Count);
        }

        var userIndex = activeItems.FindIndex(item => item.UserId == userId);

        if (userIndex < 0)
        {
            return new QueueStatusDto(
                queue.Id,
                queue.Name,
                activeItems.Count);
        }

        var userItem = activeItems[userIndex];
        var peopleAhead = userIndex;

        return new QueueStatusDto(
            queue.Id,
            queue.Name,
            activeItems.Count,
            userIndex + 1,
            peopleAhead,
            peopleAhead * queue.EstimatedTimePerItem,
            userItem.Status);
    }

    public async Task<List<QueueStatusDto>> GetUserQueuesStatusAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var queueIds = await context.QueueItems
            .AsNoTracking()
            .Where(item =>
                item.UserId == userId &&
                (item.Status == QueueItemStatus.Waiting ||
                 item.Status == QueueItemStatus.InProgress))
            .Select(item => item.QueueId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (queueIds.Count == 0)
        {
            return [];
        }

        var queues = await context.Queues
            .AsNoTracking()
            .Where(queue => queueIds.Contains(queue.Id))
            .Select(queue => new
            {
                queue.Id,
                queue.Name,
                queue.EstimatedTimePerItem
            })
            .ToListAsync(cancellationToken);

        var activeItems = await context.QueueItems
            .AsNoTracking()
            .Where(item =>
                queueIds.Contains(item.QueueId) &&
                (item.Status == QueueItemStatus.Waiting ||
                 item.Status == QueueItemStatus.InProgress))
            .OrderBy(item => item.QueueId)
            .ThenBy(item => item.Position)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.QueueId,
                item.UserId,
                item.Status
            })
            .ToListAsync(cancellationToken);

        var result = new List<QueueStatusDto>(queues.Count);

        foreach (var queue in queues)
        {
            var queueItems = activeItems
                .Where(item => item.QueueId == queue.Id)
                .ToList();

            var userIndex = queueItems.FindIndex(item => item.UserId == userId);
            if (userIndex < 0)
            {
                continue;
            }

            var peopleAhead = userIndex;

            result.Add(new QueueStatusDto(
                queue.Id,
                queue.Name,
                queueItems.Count,
                userIndex + 1,
                peopleAhead,
                peopleAhead * queue.EstimatedTimePerItem,
                queueItems[userIndex].Status));
        }

        return result;
    }

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    private async Task<QueueEntity> GetOwnedQueueAsync(
        int queueId,
        int userId,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues
            .SingleOrDefaultAsync(queue => queue.Id == queueId, cancellationToken)
            ?? throw new NotFoundException($"Queue {queueId} was not found.");

        if (queue.OwnerId != userId)
        {
            throw new ForbiddenException("Only the queue owner can perform this operation.");
        }

        return queue;
    }

    private async Task ReorderQueueAsync(
        int queueId,
        CancellationToken cancellationToken)
    {
        var inProgressCount = await context.QueueItems.CountAsync(
            item =>
                item.QueueId == queueId &&
                item.Status == QueueItemStatus.InProgress,
            cancellationToken);

        var waitingItems = await context.QueueItems
            .Where(item =>
                item.QueueId == queueId &&
                item.Status == QueueItemStatus.Waiting)
            .OrderBy(item => item.Position)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        var position = inProgressCount + 1;

        foreach (var item in waitingItems)
        {
            item.Position = position++;
            item.UpdatedAt = UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureCanManageItem(QueueItem item, int userId)
    {
        if (item.UserId != userId && item.Queue.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Only the item owner or queue owner can modify this queue item.");
        }
    }

    private static void EnsureQueueIsActive(QueueEntity queue)
    {
        if (queue.Status != QueueStatus.Active)
        {
            throw new ConflictException("This queue is closed.");
        }
    }

    private static void EnsureItemIsActive(QueueItem item)
    {
        if (item.Status is QueueItemStatus.Completed or QueueItemStatus.Cancelled)
        {
            throw new ConflictException("Completed or cancelled queue items cannot be modified.");
        }
    }

    private static QueueItemResponseDto MapItem(
        QueueItem item,
        string queueName,
        int estimatedTimePerItem)
    {
        var peopleAhead = Math.Max(0, item.Position - 1);

        return new QueueItemResponseDto(
            item.Id,
            item.QueueId,
            queueName,
            item.ItemName,
            item.Position,
            item.Status,
            peopleAhead * estimatedTimePerItem,
            peopleAhead,
            item.JoinedAt);
    }
}
