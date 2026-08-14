using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.DTOs.Queues;
using System.Security.Claims;

namespace QueueManagement.Api.Controllers;

[Route("api/queues/{queueId}/items")]
[ApiController]
[Authorize]
public class QueueItemController(IQueueService queueService) : ControllerBase
{
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    public async Task<IActionResult> Add(int queueId, [FromBody] AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var item = await queueService.AddItemToQueueAsync(queueId, GetUserId(), dto, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { queueId }, item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int queueId, CancellationToken cancellationToken)
    {
        var items = await queueService.GetQueueItemsAsync(queueId, cancellationToken);
        return Ok(items);
    }

    [HttpPut("{itemId}")]
    public async Task<IActionResult> Update(int queueId, int itemId, [FromBody] AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var item = await queueService.UpdateQueueItemAsync(itemId, dto, cancellationToken);
        return Ok(item);
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> Remove(int queueId, int itemId, CancellationToken cancellationToken)
    {
        await queueService.RemoveItemFromQueueAsync(itemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("process-next")]
    public async Task<IActionResult> ProcessNext(int queueId, CancellationToken cancellationToken)
    {
        var item = await queueService.ProcessNextItemAsync(queueId, GetUserId(), cancellationToken);
        return Ok(item);
    }

    [HttpPost("complete-current")]
    public async Task<IActionResult> CompleteCurrent(int queueId, CancellationToken cancellationToken)
    {
        var item = await queueService.CompleteCurrentItemAsync(queueId, GetUserId(), cancellationToken);
        return Ok(item);
    }
}

[Route("api/my-queues")]
[ApiController]
[Authorize]
public class MyQueueStatusController(IQueueService queueService) : ControllerBase
{
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("status")]
    public async Task<IActionResult> GetMyStatus(CancellationToken cancellationToken)
    {
        var statuses = await queueService.GetUserQueuesStatusAsync(GetUserId(), cancellationToken);
        return Ok(statuses);
    }
}