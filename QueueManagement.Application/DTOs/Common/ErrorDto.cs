namespace QueueManagement.Application.DTOs.Common;

public sealed record ErrorDto(int Status, string Title, Dictionary<string, string[]>? Errors = null);
