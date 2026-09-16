using System.ComponentModel.DataAnnotations;

namespace DocuChat.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)]
    public string Key { get; init; } = "";

    public string Issuer { get; init; } = "DocuChatAI";
    public string Audience { get; init; } = "DocuChatAI.Client";
    public int ExpiryMinutes { get; init; } = 120;
}
