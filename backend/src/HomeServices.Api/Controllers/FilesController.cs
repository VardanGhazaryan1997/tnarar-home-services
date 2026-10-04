using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// Uploads for Portal users and staff. The browser asks for an upload link, PUTs the file
/// straight to storage, then calls "complete" so the API can check it and make a thumbnail.
/// </summary>
[ApiController]
[Authorize(Policy = AuthPolicies.SignedIn)]
[Route("api/v1/files")]
public sealed class FilesController : ControllerBase
{
    public sealed record RequestUploadRequest(string FileName, string ContentType, long Size);

    /// <summary>Step 1: a signed upload link for one file.</summary>
    [HttpPost("uploads")]
    public Task<UploadTicket> RequestUpload(
        RequestUploadRequest request,
        [FromServices] ICommandHandler<RequestUpload, UploadTicket> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new RequestUpload(request.FileName, request.ContentType, request.Size), cancellationToken);

    /// <summary>Step 2: after the PUT succeeded. Safe to call again.</summary>
    [HttpPost("{id:guid}/complete")]
    public Task<FileDto> Complete(
        Guid id,
        [FromServices] ICommandHandler<CompleteUpload, FileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new CompleteUpload(id), cancellationToken);

    /// <summary>A file with fresh download links (they expire, so ask again rather than storing them).</summary>
    [HttpGet("{id:guid}")]
    public Task<FileDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetFile, FileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetFile(id), cancellationToken);
}
