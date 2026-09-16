using System.Text.Json;
using DocuChat.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuChat.Api.Controllers;

[ApiController, Authorize, Route("api/chat")]
public sealed class ChatController(IRagService rag, ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost("ask")]
    public async Task Ask(AskRequest request, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var context = await rag.PrepareAsync(userId, request, cancellationToken);
        Response.StatusCode = 200;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await SendEventAsync("sources", context.Sources, cancellationToken);
        try
        {
            await foreach (var token in rag.StreamAnswerAsync(userId, context, cancellationToken))
                await SendEventAsync("token", token, cancellationToken);
            await SendEventAsync("done", new { }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Chat stream failed for user {UserId}", userId);
            await SendEventAsync("error", new { message = "The answer stream was interrupted. Please try again." }, cancellationToken);
        }
    }

    private async Task SendEventAsync<T>(string eventName, T data, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\ndata: {JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
