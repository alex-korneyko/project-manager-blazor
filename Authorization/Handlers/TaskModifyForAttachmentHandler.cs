using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

//Can upload/delete files - project owner or task author

public class TaskModifyForAttachmentHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<TaskModifyRequirement, TaskAttachment>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TaskModifyRequirement requirement,
        TaskAttachment attachment)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        var canModify = await dbContext.Tasks
            .Where(t => t.Id == attachment.TaskItemId)
            .AnyAsync(t => t.AuthorId == userId || dbContext.Projects.Any(p => p.Id == t.ProjectId && p.OwnerId == userId));

        if (canModify)
            context.Succeed(requirement);
    }
}
