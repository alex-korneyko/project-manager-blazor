using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Data.Models;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Components.Pages;

public partial class ProjectDetails : ComponentBase
{
    private Project? _project;
    private bool _loading = true;
    private bool _isOwner;
    private string? _ownerEmail;
    private List<ProjectMember> _members = new();

    private string _inviteEmail = "";
    private string? _inviteError;
    private string? _inviteOk;

    [Parameter] public Guid ProjectId { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = null!;
    [Inject] private ApplicationDbContext DbContext { get; set; } = null!;
    [Inject] private ILogger<ProjectDetails> Logger { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;

        var project = await DbContext.Projects
            .Include(prj => prj.Members)
            .ThenInclude(member => member.User)
            .Include(prj => prj.Owner)
            .FirstOrDefaultAsync(prj => prj.Id == ProjectId);

        if (project is null)
        {
            _project = null;
            _loading = false;
            return;
        }

        var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;

        var memberResult = await AuthorizationService.AuthorizeAsync(user, project, "IsProjectMember");
        if (!memberResult.Succeeded)
        {
            _project = null;
            _loading = false;
            return;
        }

        _project = project;
        _ownerEmail = project!.Owner.Email;
        _members = project.Members.OrderBy(member => member.User.Email).ToList();

        var ownerResult = await AuthorizationService.AuthorizeAsync(user, project, "IsProjectOwner");
        _isOwner = ownerResult.Succeeded;

        _loading = false;
    }

    private async Task Invite()
    {
        _inviteError = _inviteOk = null;

        try
        {
            if (_project is null) return;

            // Ещё раз проверим право владельца
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var ownerCheck = await AuthorizationService.AuthorizeAsync(user, _project, "IsProjectOwner");
            if (!ownerCheck.Succeeded) { _inviteError = "You do not have rights"; return; }

            var email = _inviteEmail.Trim();
            if (string.IsNullOrWhiteSpace(email)) { _inviteError = "Enter Email"; return; }

            // Найдём пользователя
            // var userMgr = _dbContext.GetService<UserManager<ApplicationUser>>();
            var target = await UserManager.FindByEmailAsync(email);
            if (target is null) { _inviteError = "User with entered Email not found."; return; }

            // Проверим дубликаты
            var exists = await DbContext.ProjectMembers.AnyAsync(m => m.ProjectId == _project.Id && m.UserId == target.Id);
            if (exists) { _inviteError = "User already added"; return; }

            DbContext.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = _project.Id,
                UserId = target.Id,
                Role = "Member"
            });
            await DbContext.SaveChangesAsync();

            // Refresh list
            await OnParametersSetAsync();
            _inviteEmail = "";
            _inviteOk = "Member invited.";
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to invite user");
            _inviteError = "Failed to invite user";
        }
    }

    private async Task RemoveMember(Guid projectMemberId)
    {
        _inviteError = _inviteOk = null;

        try
        {
            if (_project is null) return;

            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
            var ownerCheck = await AuthorizationService.AuthorizeAsync(user, _project, "IsProjectOwner");
            if (!ownerCheck.Succeeded) { _inviteError = "No rights to delete."; return; }

            var pm = await DbContext.ProjectMembers.FirstOrDefaultAsync(x => x.Id == projectMemberId && x.ProjectId == _project.Id);
            if (pm is null) { _inviteError = "Member not found."; return; }

            DbContext.ProjectMembers.Remove(pm);
            await DbContext.SaveChangesAsync();

            await OnParametersSetAsync();
            _inviteOk = "Member removed.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Remove member failed");
            _inviteError = "Remove member failed.";
        }
    }
}
