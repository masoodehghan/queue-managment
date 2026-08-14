using Microsoft.AspNetCore.Identity;

namespace QueueManagement.Domain.Entities.Users;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Queues.Queue> Queues { get; set; } = new List<Queues.Queue>();
}
