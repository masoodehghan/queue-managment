using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.DTOs.Queues;
using System.Security.Claims;

namespace QueueManagement.Api.Controllers;

[Route("api/queues")]
[ApiController]
[Authorize]
public class QueueController(IQueueService queueService) : ControllerBase
{
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQueueDto dto, CancellationToken cancellationToken)
    {
        var queue = await queueService.CreateQueueAsync(GetUserId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = queue.Id }, queue);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var queues = await queueService.GetUserQueuesAsync(GetUserId(), cancellationToken);
        return Ok(queues);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var queue = await queueService.GetQueueByIdAsync(id, cancellationToken);
        return Ok(queue);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQueueDto dto, CancellationToken cancellationToken)
    {
        var queue = await queueService.UpdateQueueAsync(id, GetUserId(), dto, cancellationToken);
        return Ok(queue);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await queueService.DeleteQueueAsync(id, GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(int id, CancellationToken cancellationToken)
    {
        var status = await queueService.GetQueueStatusAsync(id, cancellationToken, GetUserId());
        return Ok(status);
    }
}