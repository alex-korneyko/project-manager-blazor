using ProjectManager.Components.UiModels;

namespace ProjectManager.Components.Common;

public class ToolBarService
{
    private ToolBar? _toolBar;
    public void Init(ToolBar toolBar)
    {
        _toolBar = toolBar;
    }

    public void InserTools(List<ToolBarButtonModel> toolBarButtons)
    {
        _toolBar?.InserTools(toolBarButtons);
    }
}
