using Microsoft.EntityFrameworkCore;
using QueueManagement.Domain.Entities.Queues;
using QueueEntity = QueueManagement.Domain.Entities.Queues.Queue;

namespace QueueManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<QueueEntity> Queues { get; }

    DbSet<QueueItem> QueueItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
