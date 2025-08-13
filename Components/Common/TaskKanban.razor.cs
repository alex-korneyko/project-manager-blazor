using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;
using TaskStatus = ProjectManager.Domain.Entities.TaskStatus;

namespace ProjectManager.Components.Common;

public partial class TaskKanban : ComponentBase
{
    [Parameter] public Guid ProjectId { get; set; }

    private readonly TaskStatus[] _columnsOrder =
    [
        TaskStatus.Backlog, TaskStatus.InProgress, TaskStatus.Blocked, TaskStatus.Done
    ];

    private readonly Dictionary<TaskStatus, bool> _hover = new()
    {
        { TaskStatus.Backlog, false }, { TaskStatus.InProgress, false },
        { TaskStatus.Blocked, false }, { TaskStatus.Done, false }
    };

    private readonly Dictionary<TaskStatus, List<TaskItem>> _columns = new();
    private readonly HashSet<Guid> _busyTasks = new();
    private Dictionary<string, string> _usersById = new();
    private Guid? _draggingId;
    private string? _error;

    // --- Modal state ---
    private bool _showModal;
    private TaskStatus _modalStatus;
    private TaskEditModel _modalModel = new();
    private string? _modalError;
    private bool _submitting;

    [Inject] private ApplicationDbContext Db { get; set; } = null!;
    [Inject] private IAuthorizationService Authz { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
    [Inject] private ILogger<TaskKanban> Log { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        await ReloadAsync();
    }

    // Позволяет странице-родителю вручную обновить доску (после создания задачи).
    public async Task ReloadAsync()
    {
        _error = null;

        foreach (var s in _columnsOrder)
            _columns[s] = new List<TaskItem>();

        var tasks = await Db.Tasks
            .Where(t => t.ProjectId == ProjectId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

        foreach (var t in tasks)
            _columns[t.Status].Add(t);

        var authorIds = tasks.Select(t => t.AuthorId).Distinct().ToList();
        var users = await Db.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();
        _usersById = users.ToDictionary(x => x.Id, x => x.Email ?? x.Id);

        StateHasChanged();
    }

    private void OnDragStart(Guid taskId)
    {
        _draggingId = taskId;
        _error = null;
    }

    private async Task OnDrop(TaskStatus target)
    {
        if (_draggingId is null) return;

        try
        {
            var t = FindTask(_draggingId.Value);
            if (t is null) return;

            if (t.Status == target) return; // ничего не меняем

            // Авторизация: менять статус может любой участник проекта
            var user = (await AuthState.GetAuthenticationStateAsync()).User;
            var auth = await Authz.AuthorizeAsync(user, t, "IsProjectMember");
            if (!auth.Succeeded)
            {
                _error = "Нет прав для смены статуса.";
                return;
            }

            // Оптимистичное обновление UI
            MoveCardInMemory(t, target);
            _busyTasks.Add(t.Id);
            StateHasChanged();

            // Персист в БД (t уже трекается DbContext-ом)
            t.Status = target;
            await Db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Kanban drop failed");
            _error = "Не удалось изменить статус.";
            // Попробуем жестко перечитать модель (на случай рассинхронизации)
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

    // --- Modal helpers ---
    private void OpenCreateModalFor(TaskStatus target)
    {
        _modalStatus = target;
        _modalModel = new TaskEditModel();
        _modalError = null;
        _showModal = true;
    }

    private void CloseModal()
    {
        _showModal = false;
        _modalError = null;
    }

    private async Task CreateTaskInModalAsync()
    {
        if (string.IsNullOrWhiteSpace(_modalModel.Title)) return;

        try
        {
            _submitting = true;
            _modalError = null;

            var user = (await AuthState.GetAuthenticationStateAsync()).User;

            var project = await Db.Projects.FirstOrDefaultAsync(p => p.Id == ProjectId);
            if (project is null)
            {
                _modalError = "Project not found."; return;
            }

            var memberResult = await Authz.AuthorizeAsync(user, project, "IsProjectMember");
            if (!memberResult.Succeeded)
            {
                _modalError = "No rights for task creation."; return;
            }

            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = ProjectId,
                Title = _modalModel.Title.Trim(),
                DescriptionMarkdown = string.IsNullOrWhiteSpace(_modalModel.DescriptionMarkdown) ? null : _modalModel.DescriptionMarkdown!.Trim(),
                Status = _modalStatus,
                AuthorId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            Db.Tasks.Add(task);
            await Db.SaveChangesAsync();

            _columns[_modalStatus].Insert(0, task);

            CloseModal();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Create task modal failed");
            _modalError = "Create task modal failed";
        }
        finally
        {
            _submitting = false;
            StateHasChanged();
        }
    }

    private void OnDragEnter(TaskStatus col) => _hover[col] = true;
    private void OnDragLeave(TaskStatus col) => _hover[col] = false;

    private sealed class TaskEditModel
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(2)]
        public string Title { get; set; } = string.Empty;
        public string? DescriptionMarkdown { get; set; }
    }
}
