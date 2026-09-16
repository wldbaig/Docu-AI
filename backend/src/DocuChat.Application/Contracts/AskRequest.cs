using System.ComponentModel.DataAnnotations;

namespace DocuChat.Application;

public sealed record AskRequest(
    [Required, MinLength(2), MaxLength(2000)] string Question,
    int TopK = 5,
    IReadOnlyList<Guid>? DocumentIds = null);
