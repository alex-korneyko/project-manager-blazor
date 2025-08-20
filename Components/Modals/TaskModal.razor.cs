using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Components.Modals.Modal;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Components.Modals;

public partial class TaskModal : ComponentBase, IModal<Guid, TaskItem>
{
    private ModalDialog _modalDialog = null!;
    private TaskItem? _task;
    private bool _canModify;
    private string? _error;
    private bool _editMode;
    private TaskEditModel _model = new();

    [Inject] IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IAuthorizationService Authz { get; set; } = null!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = null!;
    [Inject] private ILogger<EditTaskModal> Log { get; set; } = null!;

    [Parameter] public EventCallback<TaskItem> OnModalActionSucceeded { get; set; }
    [Parameter] public EventCallback<string> OnModalActionFailed { get; set; }
    [Parameter] public Guid TaskId { get; set; }

    private sealed class TaskEditModel
    {
        [Required, MinLength(2)] public string Title { get; set; } = "";
        public string? DescriptionMarkdown { get; set; }
    }

    protected override async Task OnParametersSetAsync()
    {
        _error = null;

        if (TaskId == Guid.Empty)
        {
            return;
        }

        var dbContext = await DbContextFactory.CreateDbContextAsync();
        _task = await dbContext.Tasks.Include(task => task.Attachments).FirstOrDefaultAsync(task => task.Id == TaskId);
        if (_task is null) { _error = "Task not found."; return; }

        var user = (await Auth.GetAuthenticationStateAsync()).User;
        _canModify = (await Authz.AuthorizeAsync(user, _task, "CanTaskModify")).Succeeded;

        _model = new() { Title = _task.Title, DescriptionMarkdown = _task.DescriptionMarkdown };
    }

    private async Task Save()
    {
        try
        {
            var dbContext = await DbContextFactory.CreateDbContextAsync();
            if (!_canModify || _task is null) { _error = "Not allowed."; return; }
            _task.Title = _model.Title.Trim();
            _task.DescriptionMarkdown = string.IsNullOrWhiteSpace(_model.DescriptionMarkdown) ? null : _model.DescriptionMarkdown!.Trim();
            dbContext.Update(_task);
            await dbContext.SaveChangesAsync();
            await OnModalActionSucceeded.InvokeAsync(_task);
            _editMode = false;
        }
        catch (Exception ex) { Log.LogError(ex, "Save failed"); _error = "Save failed."; }
    }

    public async Task OpenModalAsync(Guid taskId = default)
    {
        TaskId = taskId;
        _error = null;

        var dbContext = await DbContextFactory.CreateDbContextAsync();
        _task = await dbContext.Tasks.Include(task => task.Attachments).FirstOrDefaultAsync(task => task.Id == TaskId);
        if (_task is null) { _error = "Task not found."; return; }

        var user = (await Auth.GetAuthenticationStateAsync()).User;
        _canModify = (await Authz.AuthorizeAsync(user, _task, "CanTaskModify")).Succeeded;

        _model = new() { Title = _task.Title, DescriptionMarkdown = _task.DescriptionMarkdown };

        await _modalDialog.OpenModalAsync();
    }

    public void CloseModal()
    {
        _modalDialog.CloseModal();
    }
}
