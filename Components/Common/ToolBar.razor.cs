using Microsoft.AspNetCore.Components;

namespace ProjectManager.Components.Common;

public partial class ToolBar : ComponentBase
{
    [Parameter] public RenderFragment? Left { get; set; }
    [Parameter] public RenderFragment? Middle { get; set; }
    [Parameter] public RenderFragment? Right { get; set; }
}
