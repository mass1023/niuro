namespace Loans.Application;

public sealed class SsnBlacklistOptions
{
    public const string SectionName = "SsnBlacklist";

    public string[] Ssns { get; set; } = Array.Empty<string>();


}