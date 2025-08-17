using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

public class TaskModifyHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory) : AuthorizationHandler<TaskModifyRequirement, TaskItem>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TaskModifyRequirement requirement,
        TaskItem resource)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        if (resource.AuthorId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        var isOwner = await dbContext.Projects
            .AnyAsync(project => project.Id == resource.ProjectId && project.OwnerId == userId);

        if (isOwner)
            context.Succeed(requirement);
    }
}
