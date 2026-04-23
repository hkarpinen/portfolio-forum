using System.Text.Json;
using Client.Authorization;
using Client.Extensions;
using Forum.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Client.Controllers;

[ApiController]
[Route("api/forum/notifications")]
[EnableRateLimiting("standard")]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    public IActionResult List()
    {
        var empty = new NotificationStreamDto(Array.Empty<NotificationStreamEventDto>(), ContinuationToken: null);
        return Ok(empty);
    }

    [HttpPut("{eventId:guid}/read")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("write")]
    public IActionResult MarkRead([FromRoute] Guid eventId)
    {
        _ = eventId;
        return NoContent();
    }

    [HttpGet("stream")]
    [Authorize(Policy = ForumAuthorizationPolicies.MemberOrAbove)]
    [EnableRateLimiting("sse")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var payload = JsonSerializer.Serialize(new NotificationStreamEventDto(
                    EventId: Guid.NewGuid(),
                    RecipientUserId: userId,
                    EventType: "notification.ping",
                    Title: "Ping",
                    Message: "Notification stream heartbeat",
                    DeepLink: null,
                    OccurredAt: DateTime.UtcNow,
                    IsRead: false), jsonOptions);

                await Response.WriteAsync("event: notification\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected.
        }
    }
}
