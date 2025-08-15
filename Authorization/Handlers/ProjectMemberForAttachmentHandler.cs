using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

//Can reed/download files - any project member

public class ProjectMemberForAttachmentHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<ProjectMemberRequirement, TaskAttachment>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectMemberRequirement requirement,
        TaskAttachment attachment)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        var isMember = await dbContext.Tasks
            .Where(task => task.Id == attachment.TaskItemId)
            .AnyAsync(t => dbContext.Projects.Any(p => p.Id == t.ProjectId &&
                                                (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))));

        if (isMember)
            context.Succeed(requirement);
    }
}
