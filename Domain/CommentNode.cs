using ProjectManager.Domain.Entities;

namespace ProjectManager.Domain;

public class CommentNode
{
    public TaskComment Comment { get; set; } = null!;
    public string? AuthorEmail { get; set; }
    public List<CommentNode> Children { get; set; } = new();
}
