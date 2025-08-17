using ProjectManager.Data.Models;

namespace ProjectManager.Domain.Entities;

public class TaskComment
{
    public Guid Id { get; set; }

    public Guid TaskItemId { get; set; }
    public TaskItem Task { get; set; } = default!;

    public string AuthorId { get; set; } = default!;
    public ApplicationUser Author { get; set; } = default!;

    public string BodyMarkdown { get; set; } = default!;

    public Guid? ParentCommentId { get; set; }
    public TaskComment? Parent { get; set; }
    public List<TaskComment> Replies { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAtUtc { get; set; }
}
