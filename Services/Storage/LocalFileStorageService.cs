using Microsoft.Extensions.Options;

namespace ProjectManager.Services.Storage;

public class LocalFileStorageService(IWebHostEnvironment env, IOptions<StorageOptions> opts) : IFileStorageService
{
    private readonly string _webRoot = env.WebRootPath;
    private readonly StorageOptions _o = opts.Value;

    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
        return relativePath.Replace('\\','/');
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult<Stream>(File.OpenRead(full));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
