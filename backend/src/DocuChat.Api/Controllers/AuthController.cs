using DocuChat.Application;
using Microsoft.AspNetCore.Mvc;

namespace DocuChat.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) => Ok(await auth.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) => Ok(await auth.LoginAsync(request, cancellationToken));
}
