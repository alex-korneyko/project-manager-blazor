using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace ProjectManager.Components.Common;

public partial class CommentEditor : ComponentBase
{
    private bool _submitting;
    private string? _error;
    private Model _model = new();

    [Parameter] public string SubmitText { get; set; } = "Send";
    [Parameter] public EventCallback<string> OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public string EditText { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        _model.Body = EditText;
    }

    private async Task OnSubmitAsync()
    {
        try
        {
            _submitting = true;
            await OnSubmit.InvokeAsync(_model.Body);
            _model = new();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally { _submitting = false; }
    }

    private sealed class Model
    {
        [Required, MinLength(1)]
        public string Body { get; set; } = "";
    }
}
