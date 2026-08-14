using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueManagement.Domain.Entities.Queues;

namespace QueueManagement.Infrastructure.Data.Configurations;

public class QueueConfiguration : IEntityTypeConfiguration<Queue>
{
    public void Configure(EntityTypeBuilder<Queue> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Name).IsRequired().HasMaxLength(100);
        builder.Property(q => q.Description).HasMaxLength(500);
        builder.HasOne(q => q.Owner).WithMany(u => u.Queues)
            .HasForeignKey(q => q.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(q => new { q.OwnerId, q.Status });
    }
}
