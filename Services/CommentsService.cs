using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Common.Security;
using ProjectManager.Data;
using ProjectManager.Domain;
using ProjectManager.Domain.Entities;
using static ProjectManager.Authorization.AuthorizationPoliciesNames;

namespace ProjectManager.Services;

public class CommentsService(
    IAuthorizationService authorizationService,
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<List<CommentNode>> GetTreeAsync(Guid taskId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var task = await dbContext.Tasks
            .Include(task => task.Project)
            .FirstOrDefaultAsync(task => task.Id == taskId, ct);

        if (task is null) return [];

        var canReed = await authorizationService.AuthorizeAsync(user, task, IsProjectMember);

        if (!canReed.Succeeded) return [];

        var comments = await dbContext.TaskComments
            .Where(comment => comment.TaskItemId == taskId)
            .AsNoTracking()
            .OrderBy(comment => comment.CreatedAtUtc)
            .ToListAsync(ct);

        var authorsIds = comments.Select(comment => comment.AuthorId).Distinct().ToList();

        var users = await dbContext.Users
            .Where(usr => authorsIds.Contains(usr.Id))
            .Select(usr => new { usr.Id, usr.Email })
            .AsNoTracking()
            .ToListAsync(ct);

        var emails = users.ToDictionary(usr => usr.Id, usr => usr.Email ?? usr.Id);
        var map = comments.ToDictionary(comment => comment.Id, comment => new CommentNode
        {
            Comment = comment,
            AuthorEmail = emails[comment.AuthorId]
        });

        var roots = new List<CommentNode>();

        foreach (var comment in comments)
        {
            if (comment.ParentCommentId is { } parentId && map.TryGetValue(parentId, out var parent))
                parent.Children.Add(map[comment.Id]);
            else
                roots.Add(map[comment.Id]);
        }

        return roots.OrderByDescending(node => node.Comment.CreatedAtUtc).ToList();
    }

    public async Task<TaskComment?> AddAsync(Guid taskId, Guid? parentId, string bodyMarkdown, ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var task = await dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == taskId, ct);
        if (task is null) return null;

        var canModify = await authorizationService.AuthorizeAsync(user, task, IsProjectMember);
        if (!canModify.Succeeded) return null;

        var userId = user.GetUserId() ?? "";
        var taskComment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            ParentCommentId = parentId,
            AuthorId = userId,
            BodyMarkdown = bodyMarkdown,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.TaskComments.Add(taskComment);
        await dbContext.SaveChangesAsync(ct);

        return taskComment;
    }

    public async Task<bool> EditAsync(Guid commentId, string bodyMarkdown, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var comment = await dbContext.TaskComments.FirstOrDefaultAsync(comment => comment.Id == commentId, ct);
        if (comment is null) return false;

        var canEdit = await authorizationService.AuthorizeAsync(user, comment, CommentAuthor);
        if (!canEdit.Succeeded) return false;

        comment.BodyMarkdown = bodyMarkdown;
        comment.EditedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> DeleteThreadAsync(Guid rootCommentId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var root = await dbContext.TaskComments.FirstOrDefaultAsync(comment => comment.Id == rootCommentId, ct);
        if (root is null) return false;

        var canDelete = await authorizationService.AuthorizeAsync(user, root, CommentAuthor);
        if (!canDelete.Succeeded) return false;

        var allTaskComments = await dbContext.TaskComments
            .Where(comment => comment.TaskItemId == root.TaskItemId)
            .ToListAsync(ct);

        var toDelete = new HashSet<Guid>();

        void Dfs(Guid id)
        {
            foreach (var guid in allTaskComments.Where(comment => comment.ParentCommentId == id).Select(comment => comment.Id).ToList())
                Dfs(guid);

            toDelete.Add(id);
        }
        Dfs(rootCommentId);

        var ordered = allTaskComments.Where(comment => toDelete.Contains(comment.Id))
            .OrderByDescending(c => c.ParentCommentId.HasValue)
            .ToList();

        dbContext.TaskComments.RemoveRange(ordered);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }

    public async Task<int> DeleteBranchAsync(Guid rootCommentId, CancellationToken ct = default)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var sql = @"
WITH cte AS (
    SELECT Id FROM TaskComments WHERE Id = @rootId
    UNION ALL
    SELECT c.Id
    FROM TaskComments c
    INNER JOIN cte p ON c.ParentCommentId = p.Id
)
DELETE FROM TaskComments WHERE Id IN (SELECT Id FROM cte);
";

        var parameter = new SqlParameter("@rootId", rootCommentId);

        return await dbContext.Database.ExecuteSqlRawAsync(sql, [parameter], ct);
    }
}
