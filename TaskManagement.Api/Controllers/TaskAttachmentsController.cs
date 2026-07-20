using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Attachments;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/tasks/{taskId:guid}/attachments")]
public sealed class TaskAttachmentsController(AttachmentService attachmentService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType<AttachmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentResponse>> Upload(
        Guid projectId,
        Guid taskId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new ValidationProblemException(new Dictionary<string, string[]>
            {
                ["file"] = ["A file is required."]
            });
        }

        // IFormFile is an ASP.NET Core type, so it is translated here; the Application
        // layer only ever sees a name, a declared content type, a length and a stream.
        await using Stream content = file.OpenReadStream();
        var upload = new FileUpload(file.FileName, file.ContentType, file.Length, content);

        AttachmentResponse attachment =
            await attachmentService.UploadAsync(projectId, taskId, upload, cancellationToken);

        return CreatedAtAction(
            nameof(Download),
            new { projectId, taskId, attachmentId = attachment.Id },
            attachment);
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyCollection<AttachmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AttachmentResponse>>> List(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AttachmentResponse> attachments =
            await attachmentService.ListAsync(projectId, taskId, cancellationToken);
        return Ok(attachments);
    }

    [HttpGet("{attachmentId:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid projectId,
        Guid taskId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        AttachmentDownload download =
            await attachmentService.DownloadAsync(projectId, taskId, attachmentId, cancellationToken);

        // The stored content type is client-declared, so the browser must not be
        // allowed to sniff or render it: nosniff plus a download disposition keeps an
        // uploaded file from executing as HTML in the API's own origin. FileStreamResult
        // disposes the stream after the response is written.
        Response.Headers.XContentTypeOptions = "nosniff";

        return File(download.Content, download.ContentType, download.FileName);
    }
}
