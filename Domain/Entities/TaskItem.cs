using ProjectManager.Data.Models;

namespace ProjectManager.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? DescriptionMarkdown { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Backlog;

    public string AuthorId { get; set; } = default!;
    public ApplicationUser Author { get; set; } = default!;

    public List<TaskAttachment> Attachments { get; set; } = new();
    public List<TaskComment> Comments { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
