namespace QueueManagement.Application.DTOs.Auth;

public sealed record RegisterDto(string Email, string Password, string FullName);
