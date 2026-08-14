using QueueManagement.Domain.Entities.Common;
using QueueManagement.Domain.Entities.Common.Enums;
using QueueManagement.Domain.Entities.Users;

namespace QueueManagement.Domain.Entities.Queues;

public class Queue : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;
    public QueueStatus Status { get; set; } = QueueStatus.Active;
    public int EstimatedTimePerItem { get; set; } = 5;
    public ICollection<QueueItem> Items { get; set; } = new List<QueueItem>();
}
