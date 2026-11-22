namespace ImportaYa.Application.Options;

public sealed class ImportOptions
{
    public const string SectionName = "ImportOptions";

    public long MaxFileSizeBytes { get; set; } = 10L * 1024 * 1024;
    public int MaxRows { get; set; } = 50_000;
    public int BatchSize { get; set; } = 200;
    public string StoragePath { get; set; } = Path.Combine("App_Data", "imports");
}
