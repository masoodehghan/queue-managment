using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueManagement.Domain.Entities.Queues;

namespace QueueManagement.Infrastructure.Data.Configurations;

public class QueueItemConfiguration : IEntityTypeConfiguration<QueueItem>
{
    public void Configure(EntityTypeBuilder<QueueItem> builder)
    {
        builder.HasKey(qi => qi.Id);
        builder.Property(qi => qi.ItemName).IsRequired().HasMaxLength(200);
        builder.HasOne(qi => qi.Queue).WithMany(q => q.Items)
            .HasForeignKey(qi => qi.QueueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(qi => qi.User).WithMany()
            .HasForeignKey(qi => qi.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(qi => new { qi.QueueId, qi.Status, qi.Position });
    }
}
