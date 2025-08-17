using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Domain.Entities;
using ProjectManager.Services.Storage;

namespace ProjectManager.Components.Common;

public partial class TaskAttachments : ComponentBase
{
    [Parameter] public TaskItem Task { get; set; } = null!;
    [Parameter] public bool CanModify { get; set; }

    private string? _error;
    private readonly List<TaskAttachment> _items = new();

    [Inject] public IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] public IFileStorageService Storage { get; set; } = null!;
    [Inject] public IAuthorizationService Authz { get; set; } = null!;
    [Inject] public AuthenticationStateProvider Auth { get; set; } = null!;
    [Inject] public ILogger<TaskAttachments> Log { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        _error = null;
        _items.Clear();
        _items.AddRange(Task.Attachments);

        await base.OnParametersSetAsync();
    }

    private string DownloadUrl(TaskAttachment a, bool inline) =>
        $"/api/FileStorage/GetAttachment/{Task.Id}/{a.Id}"  + (inline ? "?inline=true" : string.Empty);

    private static string FormatSize(long b) =>
        b >= 1024 * 1024 ? $"{b / 1024 / 1024.0:F1} MB" :
        b >= 1024 ? $"{b / 1024.0:F1} KB" : $"{b} B";

    private static bool IsImage(TaskAttachment a) =>
        a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private async Task OnFilesSelected(InputFileChangeEventArgs e)
    {
        try
        {
            if (!CanModify)
            {
                _error = "Not allowed.";
                return;
            }

            var user = (await Auth.GetAuthenticationStateAsync()).User;
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var dbContext = await DbContextFactory.CreateDbContextAsync();

            foreach (var file in e.GetMultipleFiles())
            {
                using var s = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024); // 20 MB
                var safeName = string.Concat(file.Name.Split(Path.GetInvalidFileNameChars(),
                    StringSplitOptions.RemoveEmptyEntries));
                var rel = $"uploads/{Task.ProjectId}/{Task}/{Guid.NewGuid()}_{safeName}";

                await Storage.SaveAsync(s, rel);

                var att = new TaskAttachment
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = Task.Id,
                    FileName = file.Name,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    SizeBytes = file.Size,
                    StoredPath = rel,
                    UploaderId = userId,
                    UploadedAtUtc = DateTime.UtcNow
                };
                dbContext.TaskAttachments.Add(att);
                _items.Insert(0, att);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Upload failed");
            _error = "Upload failed.";
        }
    }

    private async Task DeleteAsync(TaskAttachment a)
    {
        try
        {
            var dbContext = await DbContextFactory.CreateDbContextAsync();

            if (!CanModify)
            {
                _error = "Not allowed.";
                return;
            }

            await Storage.DeleteAsync(a.StoredPath);
            dbContext.TaskAttachments.Remove(a);
            await dbContext.SaveChangesAsync();
            _items.RemoveAll(x => x.Id == a.Id);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Delete failed");
            _error = "Delete failed.";
        }
    }
}
