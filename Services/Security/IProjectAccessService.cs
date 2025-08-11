using ProjectManager.Domain.Entities;

namespace ProjectManager.Services.Security;

public interface IProjectAccessService
{
    Task<bool> IsProjectMemberAsync(Guid projectId, string userId, CancellationToken ct = default);
    Task<bool> IsProjectOwnerAsync(Guid projectId, string userId, CancellationToken ct = default);
    Task<bool> IsCommentOwnerAsync(Guid commentId, string userId, CancellationToken ct = default);

    Task<Project?> GetProjectIfVisibleAsync(Guid projectId, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetVisibleProjectsAsync(string userId, CancellationToken ct = default);
}
