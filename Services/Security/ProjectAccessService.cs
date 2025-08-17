using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Services.Security;

public class ProjectAccessService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IProjectAccessService
{
    public async Task<bool> IsProjectMemberAsync(Guid projectId, string userId, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.Projects
            .Where(project => project.Id == projectId)
            .AnyAsync(project => project.OwnerId == userId || project.Members.Any(member => member.UserId == userId), ct);
    }

    public async Task<bool> IsProjectOwnerAsync(Guid projectId, string userId, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == userId, ct);
    }

    public async Task<bool> IsCommentAuthorAsync(Guid commentId, string userId, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.TaskComments.AnyAsync(comment => comment.Id == commentId && comment.AuthorId == userId, ct);
    }

    public async Task<Project?> GetProjectIfVisibleAsync(Guid projectId, string userId, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.Projects
            .Where(project => project.Id == projectId && (project.OwnerId == userId || project.Members.Any(member => member.UserId == userId)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetVisibleProjectsAsync(string userId, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.Projects
            .Where(project => project.OwnerId == userId || project.Members.Any(member => member.UserId == userId))
            .ToListAsync(ct);
    }
}
