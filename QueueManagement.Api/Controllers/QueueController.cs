using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueManagement.Api.Extensions;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Application.Common.Interfaces;

namespace QueueManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/queues")]
public sealed class QueueController(
    IQueueService queueService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<QueueResponseDto>> Create(
        CreateQueueDto dto,
        CancellationToken cancellationToken)
    {
        var queue = await queueService.CreateQueueAsync(
            User.GetRequiredUserId(),
            dto,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = queue.Id },
            queue);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<QueueResponseDto>>> GetMine(
        CancellationToken cancellationToken)
    {
        var queues = await queueService.GetUserQueuesAsync(
            User.GetRequiredUserId(),
            cancellationToken);

        return Ok(queues);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QueueResponseDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var queue = await queueService.GetQueueByIdAsync(
            id,
            cancellationToken);

        return Ok(queue);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<QueueResponseDto>> Update(
        int id,
        UpdateQueueDto dto,
        CancellationToken cancellationToken)
    {
        var queue = await queueService.UpdateQueueAsync(
            id,
            User.GetRequiredUserId(),
            dto,
            cancellationToken);

        return Ok(queue);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Close(
        int id,
        CancellationToken cancellationToken)
    {
        await queueService.DeleteQueueAsync(
            id,
            User.GetRequiredUserId(),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:int}/status")]
    public async Task<ActionResult<QueueStatusDto>> GetStatus(
        int id,
        CancellationToken cancellationToken)
    {
        var status = await queueService.GetQueueStatusAsync(
            id,
            cancellationToken,
            User.GetRequiredUserId());

        return Ok(status);
    }
}
