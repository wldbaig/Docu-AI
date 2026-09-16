using System.ComponentModel.DataAnnotations;

namespace DocuChat.Application;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
