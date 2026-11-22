namespace ImportaYa.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ImportaYa";
    public string Audience { get; set; } = "ImportaYa.Client";
    public int ExpiresInMinutes { get; set; } = 60;
}
