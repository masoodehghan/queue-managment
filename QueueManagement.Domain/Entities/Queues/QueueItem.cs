using QueueManagement.Domain.Entities.Common;
using QueueManagement.Domain.Entities.Common.Enums;
using QueueManagement.Domain.Entities.Users;

namespace QueueManagement.Domain.Entities.Queues;

public class QueueItem : BaseEntity
{
    public int QueueId { get; set; }
    public Queue Queue { get; set; } = null!;
    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Position { get; set; }
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Waiting;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
