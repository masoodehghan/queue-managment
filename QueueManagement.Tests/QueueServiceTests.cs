using Microsoft.EntityFrameworkCore;
using QueueManagement.Application.Common.Exceptions;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Application.Services;
using QueueManagement.Domain.Entities.Common.Enums;
using QueueManagement.Domain.Entities.Queues;
using QueueManagement.Domain.Entities.Users;
using QueueManagement.Infrastructure.Data;
using Xunit;

namespace QueueManagement.Tests;

public class QueueServiceTests
{
    [Fact]
    public async Task AddItemToQueueAsync_AppendsItemAndCalculatesWaitingTime()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context, estimatedTimePerItem: 7);

        var firstItem = new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            UserId = owner.Id,
            ItemName = "First",
            Position = 1,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.Add(firstItem);
        await context.SaveChangesAsync();

        var joiningUser = await AddUserAsync(context, "Joining User", "joining@example.com");
        var service = new QueueService(context);

        var result = await service.AddItemToQueueAsync(
            queue.Id,
            joiningUser.Id,
            new AddQueueItemDto("Second"),
            CancellationToken.None);

        Assert.Equal(2, result.Position);
        Assert.Equal(1, result.PeopleAhead);
        Assert.Equal(7, result.EstimatedWaitingMinutes);
        Assert.Equal(QueueItemStatus.Waiting, result.Status);

        var stored = await context.QueueItems.SingleAsync(x => x.Id == result.Id);
        Assert.Equal(joiningUser.Id, stored.UserId);
        Assert.Equal("Second", stored.ItemName);
    }

    [Fact]
    public async Task ProcessNextItemAsync_ProcessesLowestPositionWaitingItem()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);

        var second = new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            ItemName = "Second",
            Position = 2,
            Status = QueueItemStatus.Waiting
        };

        var first = new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            ItemName = "First",
            Position = 1,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.AddRange(second, first);
        await context.SaveChangesAsync();

        var service = new QueueService(context);

        var result = await service.ProcessNextItemAsync(
            queue.Id,
            owner.Id,
            CancellationToken.None);

        Assert.Equal(first.Id, result.Id);
        Assert.Equal(QueueItemStatus.InProgress, result.Status);

        var storedSecond = await context.QueueItems.SingleAsync(x => x.Id == second.Id);
        Assert.Equal(QueueItemStatus.Waiting, storedSecond.Status);
    }

    [Fact]
    public async Task ProcessNextItemAsync_WhenCallerIsNotOwner_ThrowsUnauthorizedException()
    {
        await using var context = CreateContext();
        var (_, queue) = await SeedQueueAsync(context);
        var otherUser = await AddUserAsync(context, "Other User", "other@example.com");

        context.QueueItems.Add(new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            ItemName = "Waiting item",
            Position = 1,
            Status = QueueItemStatus.Waiting
        });
        await context.SaveChangesAsync();

        var service = new QueueService(context);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.ProcessNextItemAsync(
                queue.Id,
                otherUser.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task CompleteCurrentItemAsync_CompletesCurrentAndReordersWaitingItems()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context);

        var current = new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            ItemName = "Current",
            Position = 1,
            Status = QueueItemStatus.InProgress
        };

        var third = new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            ItemName = "Third",
            Position = 3,
            Status = QueueItemStatus.Waiting
        };

        var second = new QueueItem
        {
            QueueId = queue.Id,
            Queue = queue,
            ItemName = "Second",
            Position = 2,
            Status = QueueItemStatus.Waiting
        };

        context.QueueItems.AddRange(current, third, second);
        await context.SaveChangesAsync();

        var service = new QueueService(context);

        var result = await service.CompleteCurrentItemAsync(
            queue.Id,
            owner.Id,
            CancellationToken.None);

        Assert.Equal(current.Id, result.Id);
        Assert.Equal(QueueItemStatus.Completed, result.Status);

        var completed = await context.QueueItems.SingleAsync(x => x.Id == current.Id);
        Assert.NotNull(completed.CompletedAt);

        var waitingPositions = await context.QueueItems
            .Where(x => x.QueueId == queue.Id && x.Status == QueueItemStatus.Waiting)
            .OrderBy(x => x.Position)
            .Select(x => x.Position)
            .ToArrayAsync();

        Assert.Equal(new[] { 1, 2 }, waitingPositions);
    }

    [Fact]
    public async Task GetQueueStatusAsync_ReturnsUserPositionPeopleAheadAndEstimatedWait()
    {
        await using var context = CreateContext();
        var (owner, queue) = await SeedQueueAsync(context, estimatedTimePerItem: 5);
        var userAhead = await AddUserAsync(context, "Ahead User", "ahead@example.com");
        var targetUser = await AddUserAsync(context, "Target User", "target@example.com");

        context.QueueItems.AddRange(
            new QueueItem
            {
                QueueId = queue.Id,
                Queue = queue,
                UserId = owner.Id,
                ItemName = "Current",
                Position = 1,
                Status = QueueItemStatus.InProgress
            },
            new QueueItem
            {
                QueueId = queue.Id,
                Queue = queue,
                UserId = userAhead.Id,
                ItemName = "Ahead",
                Position = 2,
                Status = QueueItemStatus.Waiting
            },
            new QueueItem
            {
                QueueId = queue.Id,
                Queue = queue,
                UserId = targetUser.Id,
                ItemName = "Target",
                Position = 3,
                Status = QueueItemStatus.Waiting
            });

        await context.SaveChangesAsync();

        var service = new QueueService(context);

        var result = await service.GetQueueStatusAsync(
            queue.Id,
            CancellationToken.None,
            targetUser.Id);

        Assert.Equal(3, result.TotalInQueue);
        Assert.Equal(3, result.YourPosition);
        Assert.Equal(2, result.PeopleAhead);
        Assert.Equal(10, result.EstimatedWaitingMinutes);
        Assert.Equal(QueueItemStatus.Waiting, result.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"queue-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(ApplicationUser Owner, Queue Queue)> SeedQueueAsync(
        AppDbContext context,
        int estimatedTimePerItem = 5)
    {
        var owner = await AddUserAsync(context, "Queue Owner", "owner@example.com");

        var queue = new Queue
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
