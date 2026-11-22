namespace ImportaYa.Application.Abstractions;

public interface IFileStorage
{
    /// <summary>Guarda el archivo y devuelve un nombre relativo seguro.</summary>
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default);
}
