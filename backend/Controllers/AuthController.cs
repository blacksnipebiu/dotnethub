
using Microsoft.AspNetCore.Mvc;
using DotNetHub.Server.Models;
using DotNetHub.Server.Services;

namespace DotNetHub.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    
    public AuthController(AuthService auth) => _auth = auth;
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var result = await _auth.Register(req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.Login(req);
        if (!result.Success) return Unauthorized(result);
        return Ok(result);
    }
}
