namespace ProjectManager.Services.Storage;

public class StorageOptions
{
    public string UploadsRootRelative { get; set; } = "uploads"; // внутри wwwroot
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB

    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf", "text/plain", "application/zip",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
}
