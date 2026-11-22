using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Options;
using Microsoft.Extensions.Options;

namespace ImportaYa.Infrastructure.Files;

public sealed class LocalFileStorage(IOptions<ImportOptions> options) : IFileStorage
{
    private string RootPath => Path.IsPathRooted(options.Value.StoragePath)
        ? options.Value.StoragePath
        : Path.Combine(AppContext.BaseDirectory, options.Value.StoragePath);

    public async Task<string> SaveAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(RootPath);

        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(RootPath, storedName);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);

        return storedName;
    }

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(RootPath, Path.GetFileName(storedPath));
        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, 81920, useAsync: true));
    }
}
