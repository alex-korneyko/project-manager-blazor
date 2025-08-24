using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using ProjectManager.Components.Modals.Modal;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Components.Modals;

public partial class ProjectMembersModal : ComponentBase, IModal<Guid?, Guid>
{
    private ModalDialog _modalDialogRef = null!;
    private Project? _project;
    private string? _error;

    [Parameter] public EventCallback<Guid> OnModalActionSucceeded { get; set; }
    [Parameter] public EventCallback<string> OnModalActionFailed { get; set; }

    [Inject] public IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private ILogger<ProjectMembersModal> Log { get; set; } = null!;

    public async Task OpenModalAsync(Guid? projectId)
    {
        var applicationDbContext = await DbContextFactory.CreateDbContextAsync();
        _project = await applicationDbContext.Projects
            .Include(prj => prj.Owner)
            .Include(prj => prj.Members)
            .ThenInclude(prjMember => prjMember.User)
            .FirstOrDefaultAsync(prj => prj.Id == projectId);

        if (_project is null)
        {
            Log.LogWarning("ProjectMembersModal failed. Project {PrjId} not found.", projectId);
            _error = "Project not found.";
            return;
        }

        await _modalDialogRef.OpenModalAsync();

        StateHasChanged();
    }

    public void CloseModal()
    {
        _modalDialogRef.CloseModal();
    }
}
