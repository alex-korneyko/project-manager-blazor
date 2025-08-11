using ProjectManager.Data.Models;

namespace ProjectManager.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public string OwnerId { get; set; } = default!;
    public ApplicationUser Owner { get; set; } = default!;

    public List<ProjectMember> Members { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
