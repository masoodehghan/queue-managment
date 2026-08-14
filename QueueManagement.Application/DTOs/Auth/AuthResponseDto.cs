namespace QueueManagement.Application.DTOs.Auth;

public sealed record AuthResponseDto(string Token, int UserId, string Email, string FullName);
