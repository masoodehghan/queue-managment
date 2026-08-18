using QueueManagement.Domain.Entities.Users;

namespace QueueManagement.Application.Common.Interfaces;

public interface IJwtTokenService
{
    int ExpirationSeconds { get; }

    string CreateToken(ApplicationUser user);
}
