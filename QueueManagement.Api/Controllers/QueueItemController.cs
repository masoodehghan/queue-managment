using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueManagement.Api.Extensions;
using QueueManagement.Application.DTOs.Queues;
using QueueManagement.Application.Common.Interfaces;

namespace QueueManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/queues/{queueId:int}/items")]
public sealed class QueueItemController(
    IQueueService queueService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<QueueItemResponseDto>> Add(
        int queueId,
        AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var item = await queueService.AddItemToQueueAsync(
            queueId,
            User.GetRequiredUserId(),
            dto,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { queueId },
            item);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<QueueItemResponseDto>>> GetAll(
        int queueId,
        CancellationToken cancellationToken)
    {
        var items = await queueService.GetQueueItemsAsync(
            queueId,
            cancellationToken);

        return Ok(items);
    }

    [HttpPut("{itemId:int}")]
    public async Task<ActionResult<QueueItemResponseDto>> Update(
        int queueId,
        int itemId,
        AddQueueItemDto dto,
        CancellationToken cancellationToken)
    {
        var item = await queueService.UpdateQueueItemAsync(
            queueId,
            itemId,
            User.GetRequiredUserId(),
            dto,
            cancellationToken);

        return Ok(item);
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> Remove(
        int queueId,
        int itemId,
        CancellationToken cancellationToken)
    {
        await queueService.RemoveItemFromQueueAsync(
            queueId,
            itemId,
            User.GetRequiredUserId(),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("process-next")]
    public async Task<ActionResult<QueueItemResponseDto>> ProcessNext(
        int queueId,
        CancellationToken cancellationToken)
    {
        var item = await queueService.ProcessNextItemAsync(
            queueId,
            User.GetRequiredUserId(),
            cancellationToken);

        return Ok(item);
    }

    [HttpPost("complete-current")]
    public async Task<ActionResult<QueueItemResponseDto>> CompleteCurrent(
        int queueId,
        CancellationToken cancellationToken)
    {
        var item = await queueService.CompleteCurrentItemAsync(
            queueId,
            User.GetRequiredUserId(),
            cancellationToken);

        return Ok(item);
    }
}
