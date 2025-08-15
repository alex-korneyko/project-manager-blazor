using ProjectManager.Data.Models;

namespace ProjectManager.Domain.Entities;

public class TaskAttachment
{
    public Guid Id { get; set; }

    public Guid TaskItemId { get; set; }
    public TaskItem Task { get; set; } = default!;

    public string FileName { get; set; } = default!;
    public string StoredPath { get; set; } = default!; // относительный путь в сторадже
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";

    public string UploaderId { get; set; } = default!;
    public ApplicationUser Uploader { get; set; } = default!;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
