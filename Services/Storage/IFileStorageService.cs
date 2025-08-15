namespace ProjectManager.Services.Storage;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}
