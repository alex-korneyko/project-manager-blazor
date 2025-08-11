using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Services.Security;

public class ProjectAccessService(ApplicationDbContext dbContext) : IProjectAccessService
{
    public async Task<bool> IsProjectMemberAsync(Guid projectId, string userId, CancellationToken ct = default)
    {
        return await dbContext.Projects
            .Where(project => project.Id == projectId)
            .AnyAsync(project => project.Owner.Id == userId || project.Members.Any(member => member.UserId == userId), ct);
    }

    public async Task<bool> IsProjectOwnerAsync(Guid projectId, string userId, CancellationToken ct = default)
    {
        return await dbContext.Projects.AnyAsync(project => project.Id == projectId && project.Owner.Id == userId, ct);
    }

    public async Task<bool> IsCommentOwnerAsync(Guid commentId, string userId, CancellationToken ct = default)
    {
        return await dbContext.TaskComments.AnyAsync(comment => comment.Id == commentId && comment.Author.Id == userId, ct);
    }

    public async Task<Project?> GetProjectIfVisibleAsync(Guid projectId, string userId, CancellationToken ct = default)
    {
        return await dbContext.Projects
            .Where(project => project.Id == projectId && (project.Owner.Id == userId || project.Members.Any(member => member.UserId == userId)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetVisibleProjectsAsync(string userId, CancellationToken ct = default)
    {
        return await dbContext.Projects
            .Where(project => project.Owner.Id == userId || project.Members.Any(member => member.UserId == userId)).ToListAsync(ct);
    }
}
