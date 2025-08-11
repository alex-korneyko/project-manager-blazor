using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Authorization.Handlers;

public sealed class CommentAuthorHandler : AuthorizationHandler<CommentAuthorRequirement, TaskComment>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CommentAuthorRequirement requirement,
        TaskComment resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is not null && resource.AuthorId == userId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
