using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueManagement.Api.Extensions;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Application.Common.Interfaces;

namespace QueueManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/my-queues/status")]
public sealed class MyQueueStatusController(
    IQueueService queueService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<QueueStatusDto>>> Get(
        CancellationToken cancellationToken)
    {
        var statuses = await queueService.GetUserQueuesStatusAsync(
            User.GetRequiredUserId(),
            cancellationToken);

        return Ok(statuses);
    }
}
