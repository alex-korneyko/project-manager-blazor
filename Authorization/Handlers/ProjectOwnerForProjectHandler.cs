using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

public sealed class ProjectOwnerForProjectHandler
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

public sealed class ProjectOwnerForTaskHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    : AuthorizationHandler<ProjectOwnerRequirement, TaskItem>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectOwnerRequirement requirement,
        TaskItem resource)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        var isOwner = await dbContext.Projects.AnyAsync(project => project.Id == resource.ProjectId && project.OwnerId == userId);

        if (isOwner)
            context.Succeed(requirement);
    }
}
