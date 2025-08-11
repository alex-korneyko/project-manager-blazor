using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

public sealed class ProjectMemberForProjectHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<ProjectMemberRequirement, Project>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectMemberRequirement requirement,
        Project resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return;

        var isMember = resource.OwnerId == userId
                       || await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == resource.Id && member.UserId == userId);

        if (isMember)
            context.Succeed(requirement);
    }
}

public sealed class ProjectMemberForTaskHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<ProjectMemberRequirement, TaskItem>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectMemberRequirement requirement,
        TaskItem resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return;

        var isMember = await dbContext.Projects
            .Where(project => project.Id == resource.ProjectId)
            .AnyAsync(project => project.OwnerId == userId
                               || dbContext.ProjectMembers.Any(member => member.ProjectId == project.Id
                                                                         && member.UserId == userId));

        if (isMember)
            context.Succeed(requirement);
    }
}
