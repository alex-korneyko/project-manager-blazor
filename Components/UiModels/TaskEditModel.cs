using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Components.UiModels;

public class TaskEditModel
{
    [Required, MinLength(2)]
    public string Title { get; set; } = string.Empty;
    public string? DescriptionMarkdown { get; set; }
}
