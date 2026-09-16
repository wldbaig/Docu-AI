using System.ComponentModel.DataAnnotations;

namespace DocuChat.Application;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password);
