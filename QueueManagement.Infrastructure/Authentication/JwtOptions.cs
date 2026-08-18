namespace QueueManagement.Infrastructure.Authentication;

public sealed record JwtOptions(
    string Key,
    string Issuer,
    string Audience,
    int ExpirationMinutes);
