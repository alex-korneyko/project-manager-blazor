using Microsoft.AspNetCore.Components;
using ProjectManager.Components.UiModels;

namespace ProjectManager.Components.Common;

public partial class ToolBar : ComponentBase
{
    private List<ToolBarButtonModel> _tools = new();

    [Inject] public ToolBarService ToolBarService { get; set; } = null!;

    protected override void OnInitialized()
    {
        ToolBarService.Init(this);
        base.OnInitialized();
    }

    public void InserTools(List<ToolBarButtonModel> toolBarButtons)
    {
        _tools.Clear();
        _tools.AddRange(toolBarButtons);
        StateHasChanged();
    }
}
