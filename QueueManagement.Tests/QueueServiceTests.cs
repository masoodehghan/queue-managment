using Microsoft.EntityFrameworkCore;
using QueueManagement.Application.Common.Exceptions;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Application.Services;
using QueueManagement.Domain.Entities.Common.Enums;
using QueueManagement.Domain.Entities.Queues;
using QueueManagement.Domain.Entities.Users;
using QueueManagement.Infrastructure.Data;
using Xunit;
using QueueEntity = QueueManagement.Domain.Entities.Queues.Queue;

namespace QueueManagement.Tests;

public sealed class QueueServiceTests
{
    [Fact]
    public async Task AddItemToQueueAsync_AppendsItemAndCalculatesWaitingTime()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context, estimatedTimePerItem: 7);

        context.QueueItems.Add(new QueueItem
        {
            QueueId = queue.Id,
            UserId = owner.Id,
            ItemName = "First",
            Position = 1,
            Status = QueueItemStatus.Waiting
        });
        await context.SaveChangesAsync();

        var joiningUser = await AddUserAsync(
            context,
            "Joining User",
            "joining@example.com");

        var service = CreateService(context);

        var result = await service.AddItemToQueueAsync(
            queue.Id,
            joiningUser.Id,
            new AddQueueItemDto("Second"),
            CancellationToken.None);

        Assert.Equal(2, result.Position);
        Assert.Equal(1, result.PeopleAhead);
        Assert.Equal(7, result.EstimatedWaitingMinutes);
        Assert.Equal(QueueItemStatus.Waiting, result.Status);
    }

    [Fact]
    public async Task AddItemToQueueAsync_WhenUserAlreadyHasActiveItem_ThrowsConflict()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);

        context.QueueItems.Add(new QueueItem
        {
            QueueId = queue.Id,
            UserId = owner.Id,
            ItemName = "Existing",
            Position = 1,
            Status = QueueItemStatus.Waiting
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddItemToQueueAsync(
                queue.Id,
                owner.Id,
                new AddQueueItemDto("Duplicate"),
                CancellationToken.None));
    }

    [Fact]
    public async Task ProcessNextItemAsync_ProcessesLowestPositionWaitingItem()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);

        var second = new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "Second",
            Position = 2,
            Status = QueueItemStatus.Waiting
        };

        var first = new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "First",
            Position = 1,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.AddRange(second, first);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.ProcessNextItemAsync(
            queue.Id,
            owner.Id,
            CancellationToken.None);

        Assert.Equal(first.Id, result.Id);
        Assert.Equal(QueueItemStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task ProcessNextItemAsync_WhenItemAlreadyInProgress_ThrowsConflict()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);

        context.QueueItems.AddRange(
            new QueueItem
            {
                QueueId = queue.Id,
                ItemName = "Current",
                Position = 1,
                Status = QueueItemStatus.InProgress
            },
            new QueueItem
            {
                QueueId = queue.Id,
                ItemName = "Waiting",
                Position = 2,
                Status = QueueItemStatus.Waiting
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ProcessNextItemAsync(
                queue.Id,
                owner.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task ProcessNextItemAsync_WhenCallerIsNotOwner_ThrowsForbidden()
    {
        await using var context = CreateContext();
        var (_, queue) = await SeedQueueAsync(context);
        var otherUser = await AddUserAsync(
            context,
            "Other User",
            "other@example.com");

        context.QueueItems.Add(new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "Waiting item",
            Position = 1,
            Status = QueueItemStatus.Waiting
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ProcessNextItemAsync(
                queue.Id,
                otherUser.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateQueueItemAsync_WhenCallerOwnsNeitherItemNorQueue_ThrowsForbidden()
    {
        await using var context = CreateContext();
        var (_, queue) = await SeedQueueAsync(context);
        var itemOwner = await AddUserAsync(
            context,
            "Item Owner",
            "item-owner@example.com");
        var stranger = await AddUserAsync(
            context,
            "Stranger",
            "stranger@example.com");

        var item = new QueueItem
        {
            QueueId = queue.Id,
            UserId = itemOwner.Id,
            ItemName = "Owned item",
            Position = 1,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.Add(item);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateQueueItemAsync(
                queue.Id,
                item.Id,
                stranger.Id,
                new AddQueueItemDto("Changed"),
                CancellationToken.None));
    }

    [Fact]
    public async Task RemoveItemFromQueueAsync_WithCurrentItem_PreservesPositionOneForInProgress()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);
        var waitingOwner = await AddUserAsync(
            context,
            "Waiting Owner",
            "waiting-owner@example.com");

        var current = new QueueItem
        {
            QueueId = queue.Id,
            UserId = owner.Id,
            ItemName = "Current",
            Position = 1,
            Status = QueueItemStatus.InProgress
        };

        var removed = new QueueItem
        {
            QueueId = queue.Id,
            UserId = waitingOwner.Id,
            ItemName = "Remove me",
            Position = 2,
            Status = QueueItemStatus.Waiting
        };

        var remaining = new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "Remaining",
            Position = 3,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.AddRange(current, removed, remaining);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.RemoveItemFromQueueAsync(
            queue.Id,
            removed.Id,
            waitingOwner.Id,
            CancellationToken.None);

        var storedCurrent = await context.QueueItems.SingleAsync(
            item => item.Id == current.Id);
        var storedRemaining = await context.QueueItems.SingleAsync(
            item => item.Id == remaining.Id);

        Assert.Equal(1, storedCurrent.Position);
        Assert.Equal(2, storedRemaining.Position);
        Assert.Equal(QueueItemStatus.Cancelled,
            (await context.QueueItems.SingleAsync(item => item.Id == removed.Id)).Status);
    }

    [Fact]
    public async Task CompleteCurrentItemAsync_CompletesCurrentAndReordersWaitingItems()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);

        var current = new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "Current",
            Position = 1,
            Status = QueueItemStatus.InProgress
        };

        var third = new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "Third",
            Position = 3,
            Status = QueueItemStatus.Waiting
        };

        var second = new QueueItem
        {
            QueueId = queue.Id,
            ItemName = "Second",
            Position = 2,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.AddRange(current, third, second);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.CompleteCurrentItemAsync(
            queue.Id,
            owner.Id,
            CancellationToken.None);

        Assert.Equal(current.Id, result.Id);
        Assert.Equal(QueueItemStatus.Completed, result.Status);

        var completed = await context.QueueItems.SingleAsync(
            item => item.Id == current.Id);
        Assert.NotNull(completed.CompletedAt);

        var waitingPositions = await context.QueueItems
            .Where(item =>
                item.QueueId == queue.Id &&
                item.Status == QueueItemStatus.Waiting)
            .OrderBy(item => item.Position)
            .Select(item => item.Position)
            .ToArrayAsync();

        Assert.Equal(new[] { 1, 2 }, waitingPositions);
    }

    [Fact]
    public async Task GetQueueStatusAsync_UsesOrdinalPositionAndCalculatesWait()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(
            context,
            estimatedTimePerItem: 5);

        var target = await AddUserAsync(
            context,
            "Target User",
            "target@example.com");

        context.QueueItems.AddRange(
            new QueueItem
            {
                QueueId = queue.Id,
                UserId = owner.Id,
                ItemName = "First",
                Position = 10,
                Status = QueueItemStatus.InProgress
            },
            new QueueItem
            {
                QueueId = queue.Id,
                UserId = target.Id,
                ItemName = "Target",
                Position = 30,
                Status = QueueItemStatus.Waiting
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetQueueStatusAsync(
            queue.Id,
            CancellationToken.None,
            target.Id);

        Assert.Equal(2, result.TotalInQueue);
        Assert.Equal(2, result.YourPosition);
        Assert.Equal(1, result.PeopleAhead);
        Assert.Equal(5, result.EstimatedWaitingMinutes);
        Assert.Equal(QueueItemStatus.Waiting, result.Status);
    }

    private static QueueService CreateService(AppDbContext context) =>
        new(context, TimeProvider.System);

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"queue-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(ApplicationUser Owner, QueueEntity Queue)> SeedQueueAsync(
        AppDbContext context,
        int estimatedTimePerItem = 5)
    {
        var owner = await AddUserAsync(
            context,
            "Queue Owner",
            $"owner-{Guid.NewGuid():N}@example.com");

        var queue = new QueueEntity
        {
            Name = "Support Queue",
            Description = "Queue used by tests",
            OwnerId = owner.Id,
            Owner = owner,
            EstimatedTimePerItem = estimatedTimePerItem,
            Status = QueueStatus.Active
        };

        context.Queues.Add(queue);
        await context.SaveChangesAsync();

        return (owner, queue);
    }

    private static async Task<ApplicationUser> AddUserAsync(
        AppDbContext context,
        string fullName,
        string email)
    {
        var user = new ApplicationUser
        {
            FullName = fullName,
            Email = email,
            UserName = email
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }
}
