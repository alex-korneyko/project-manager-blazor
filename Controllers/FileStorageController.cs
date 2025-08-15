using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Services.Storage;

namespace ProjectManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileStorageController(
    ApplicationDbContext db,
    IAuthorizationService auth,
    IFileStorageService storage)
    : Controller
{

    [Authorize]
    [HttpGet("GetAttachment/{taskId:guid}/{attachmentId:guid}")]
    public async Task<IResult> GetAttachment(Guid taskId, Guid attachmentId, CancellationToken ct)
    {
        var att = await db.TaskAttachments.Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskItemId == taskId, ct);
        if (att is null) return Results.NotFound();

        // Чтение разрешено участникам проекта
        var res = await auth.AuthorizeAsync(User, att, "IsProjectMember");
        if (!res.Succeeded) return Results.Forbid();

        var stream = await storage.OpenReadAsync(att.StoredPath, ct);
        return Results.File(stream, att.ContentType, att.FileName, enableRangeProcessing: true);
    }

    [Authorize]
    [HttpGet("test")]
    public string GetInfo()
    {
        return "Hello World!";
    }
}
