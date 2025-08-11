using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

public sealed class ProjectOwnerForProjectHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<ProjectOwnerRequirement, Project>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectOwnerRequirement requirement,
        Project resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null && resource.OwnerId == userId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class ProjectOwnerForTaskHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<ProjectOwnerRequirement, TaskItem>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectOwnerRequirement requirement,
        TaskItem resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        var isOwner = await dbContext.Projects.AnyAsync(project => project.Id == resource.ProjectId && project.OwnerId == userId);

        if (isOwner)
            context.Succeed(requirement);
    }
}
