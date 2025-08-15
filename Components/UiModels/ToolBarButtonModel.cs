namespace ProjectManager.Components.UiModels;

public class ToolBarButtonModel
{
    public string Title { get; set; } = string.Empty;
    public string? Class { get; set; }
    public Action? ClickAction { get; set; }
}
