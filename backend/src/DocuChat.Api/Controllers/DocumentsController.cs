using DocuChat.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuChat.Api.Controllers;

[ApiController, Authorize, Route("api/documents")]
public sealed class DocumentsController(IDocumentService documents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(CancellationToken cancellationToken) => Ok(await documents.ListAsync(User.UserId(), cancellationToken));

    [HttpPost, RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await documents.UploadAsync(User.UserId(), new UploadDocumentCommand(file.FileName, file.Length, stream), cancellationToken);
        return CreatedAtAction(nameof(List), result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await documents.DeleteAsync(User.UserId(), id, cancellationToken);
        return NoContent();
    }
}
