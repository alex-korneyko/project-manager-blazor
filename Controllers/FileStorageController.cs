using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Services.Storage;
using static ProjectManager.Authorization.AuthorizationPoliciesNames;

namespace ProjectManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileStorageController(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationService auth,
    IFileStorageService storage)
    : Controller
{

    [Authorize]
    [HttpGet("GetAttachment/{taskId:guid}/{attachmentId:guid}")]
    public async Task<IActionResult> GetAttachment(Guid taskId, Guid attachmentId, [FromQuery] bool inline = false, CancellationToken ct = default)
    {
        var db = await dbContextFactory.CreateDbContextAsync(ct);
        var att = await db.TaskAttachments.Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskItemId == taskId, ct);
        if (att is null) return NotFound();

        var res = await auth.AuthorizeAsync(User, att, IsProjectMember);
        if (!res.Succeeded) return Forbid();

        var stream = await storage.OpenReadAsync(att.StoredPath, ct);

        return inline ? File(stream, att.ContentType) : File(stream, att.ContentType, att.FileName, enableRangeProcessing: true);
    }
}
