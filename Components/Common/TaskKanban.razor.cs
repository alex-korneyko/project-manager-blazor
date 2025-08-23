using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Components.Modals;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;
using static ProjectManager.Authorization.AuthorizationPoliciesNames;
using TaskStatus = ProjectManager.Domain.Entities.TaskStatus;

namespace ProjectManager.Components.Common;

public partial class TaskKanban : ComponentBase
{
    private readonly TaskStatus[] _columnsOrder =
    [
        TaskStatus.Backlog, TaskStatus.InProgress, TaskStatus.Blocked, TaskStatus.Done
    ];

    private readonly Dictionary<TaskStatus, bool> _hover = new()
    {
        { TaskStatus.Backlog, false }, { TaskStatus.InProgress, false },
        { TaskStatus.Blocked, false }, { TaskStatus.Done, false }
    };

    private Project? _currentProject;
    private readonly Dictionary<TaskStatus, List<TaskItem>> _columns = new();
    private readonly HashSet<Guid> _busyTasks = new();
    private Guid? _draggingId;
    private string? _error;

    private NewTaskModal _newTaskModal = null!;
    private TaskModal _taskModal = null!;
    private int _dragDepth;

    [Inject] public IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IAuthorizationService Authz { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
    [Inject] private ILogger<TaskKanban> Log { get; set; } = null!;

    [Parameter] public Guid ProjectId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        _error = null;
        var dbContext = await DbContextFactory.CreateDbContextAsync();

        foreach (var s in _columnsOrder)
            _columns[s] = new List<TaskItem>();

        _currentProject = await dbContext.Projects
            .Include(p => p.Tasks)
            .ThenInclude(task => task.Author)
            .FirstOrDefaultAsync(p => p.Id == ProjectId);

        if (_currentProject is null)
        {
            _error = "Project not found";
            return;
        }

        foreach (var t in _currentProject.Tasks.OrderByDescending(t => t.CreatedAtUtc))
            _columns[t.Status].Add(t);

        StateHasChanged();
    }

    private void OnDragStart(Guid taskId)
    {
        _draggingId = taskId;
        _error = null;
    }

    private async Task OnDrop(TaskStatus target)
    {
        _dragDepth = 0;

        if (_draggingId is null) return;

        try
        {
            var dbContext = await DbContextFactory.CreateDbContextAsync();
            if (_busyTasks.Contains(_draggingId.Value)) return;
            var t = FindTask(_draggingId.Value);
            if (t is null) return;

            if (t.Status == target) return;

            var user = (await AuthState.GetAuthenticationStateAsync()).User;
            var auth = await Authz.AuthorizeAsync(user, t, IsProjectMember);
            if (!auth.Succeeded)
            {
                _error = "No rights for the status change.";
                return;
            }

            MoveCardInMemory(t, target);
            _busyTasks.Add(t.Id);
            StateHasChanged();

            t.Status = target;
            dbContext.Tasks.Update(t);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Kanban drop failed");
            _error = "Kanban drop failed.";
            await ReloadAsync();
        }
        finally
        {
            if (_draggingId  is { } id)
                _busyTasks.Remove(id);

            _draggingId = null;

            _hover.Keys.ToList().ForEach(k => _hover[k] = false);

            StateHasChanged();
        }
    }

    private TaskItem? FindTask(Guid id)
    {
        foreach (var s in _columnsOrder)
        {
            var t = _columns[s].FirstOrDefault(x => x.Id == id);
            if (t is not null) return t;
        }
        return null;
    }

    private void MoveCardInMemory(TaskItem t, TaskStatus target)
    {
        _columns[t.Status].RemoveAll(x => x.Id == t.Id);
        t.Status = target; // локально меняем (оптимизм)
        _columns[target].Insert(0, t);
    }

    private void OnTaskCreated(TaskItem task)
    {
        _columns[task.Status].Insert(0, task);
        _newTaskModal.CloseModal();
        StateHasChanged();
    }

    private void OnDragEnter(TaskStatus col)
    {
        _dragDepth++;
        _hover[col] = true;
    }

    private void OnDragLeave(TaskStatus col)
    {
        _dragDepth--;
        if (_dragDepth == 0)
        {
            _hover[col] = false;
        }
    }

    private async Task OnTaskSaved(TaskItem task)
    {
        await ReloadAsync();
    }
}
