using Microsoft.AspNetCore.Components;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Components.Common;

public partial class TaskKanbanCard : ComponentBase
{
    [Parameter] public TaskItem? Task { get; set; }
    [Parameter] public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}
