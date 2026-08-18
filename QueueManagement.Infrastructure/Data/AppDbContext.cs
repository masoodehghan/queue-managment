using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Domain.Entities.Queues;
using QueueManagement.Domain.Entities.Users;
using QueueEntity = QueueManagement.Domain.Entities.Queues.Queue;

namespace QueueManagement.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options),
      IApplicationDbContext
{
    public DbSet<QueueEntity> Queues => Set<QueueEntity>();

    public DbSet<QueueItem> QueueItems => Set<QueueItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
