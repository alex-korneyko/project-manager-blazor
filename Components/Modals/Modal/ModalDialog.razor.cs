using Azure.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ProjectManager.Components.Modals.Modal;

public partial class ModalDialog : ComponentBase
{
    private bool _show;

    [Parameter] public RenderFragment? ModalDialogHeader { get; set; }
    [Parameter] [EditorRequired] public RenderFragment? ModalDialogBody { get; set; }
    [Parameter] public RenderFragment? ModalDialogFooter { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public string Height { get; set; } = "400px";
    [Parameter] public string Width { get; set; } = "500px";

    public async Task OpenModalAsync()
    {
        _show = true;
        StateHasChanged();
    }

    public void CloseModal(MouseEventArgs? args = null)
    {
        OnClose.InvokeAsync();
        _show = false;
    }
}
