using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;

namespace ProjectManager.Services;

public class CommentService
{
    private readonly ApplicationDbContext dbContext;

    public CommentService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<int> DeleteBranchAsync(Guid rootCommentId, CancellationToken ct = default)
    {
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
