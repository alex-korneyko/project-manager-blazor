using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Components.Modals;

public partial class EditTaskModal : ComponentBase, IModal<Guid, TaskItem>
{
    private bool _show;
    private TaskItem? _task;
    private bool _canModify;
    private string? _error;
    private TaskEditModel _model = new();

    [Inject] private ApplicationDbContext Db { get; set; } = null!;
    [Inject] private IAuthorizationService Authz { get; set; } = null!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = null!;
    [Inject] private ILogger<EditTaskModal> Log { get; set; } = null!;

    [Parameter] public Guid TaskId { get; set; }
    [Parameter] public EventCallback<TaskItem> OnSaved { get; set; }
    [Parameter] public EventCallback<TaskItem> OnModalActionSucceeded { get; set; }
    [Parameter] public EventCallback<string> OnModalActionFailed { get; set; }

    private sealed class TaskEditModel
    {
        [Required, MinLength(2)] public string Title { get; set; } = "";
        public string? DescriptionMarkdown { get; set; }
    }

    private async Task Save()
    {
        try
        {
            if (!_canModify || _task is null) { _error = "Not allowed."; return; }
            _task.Title = _model.Title.Trim();
            _task.DescriptionMarkdown = string.IsNullOrWhiteSpace(_model.DescriptionMarkdown) ? null : _model.DescriptionMarkdown!.Trim();
            await Db.SaveChangesAsync();
            await OnSaved.InvokeAsync(_task);
            await OnModalActionSucceeded.InvokeAsync(_task);
            CloseModal();
        }
        catch (Exception ex) { Log.LogError(ex, "Save failed"); _error = "Save failed."; }
    }

    public async Task OpenModalAsync(Guid taskId)
    {
        TaskId = taskId;
        _show = true;
        _error = null;

        _task = await Db.Tasks.Include(task => task.Attachments).FirstOrDefaultAsync(task => task.Id == TaskId);
        if (_task is null) { _error = "Task not found."; return; }

        var user = (await Auth.GetAuthenticationStateAsync()).User;
        _canModify = (await Authz.AuthorizeAsync(user, _task, "CanTaskModify")).Succeeded;

        _model = new() { Title = _task.Title, DescriptionMarkdown = _task.DescriptionMarkdown };
        StateHasChanged();
    }

    public void CloseModal() => _show = false;
}
