using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{

    [HttpPost("register")]
    public IActionResult Register()
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("login")]
    public IActionResult Login()
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
