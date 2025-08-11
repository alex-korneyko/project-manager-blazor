using ProjectManager.Data.Models;

namespace ProjectManager.Domain.Entities;

public class ProjectMember
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    // Роль на уровне проекта (не путать с глобальными ролями Identity)
    public string Role { get; set; } = "Member";
}
