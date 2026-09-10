using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Authorize]
public class InviteController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public InviteController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    [Route("api/invites/{token}")]
    public async Task<IActionResult> GetInvite(string token)
    { try { var r = await _domain.GetInviteByTokenAsync(token); return r is null ? NotFound(new { message = "Invite not found." }) : Ok(r); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    [Route("api/invites/{token}/accept")]
    public async Task<IActionResult> AcceptInvite(string token)
    { try { return (await _domain.AcceptInviteAsync(token, CurrentUserId())) ? Ok(new { message = "Invite accepted." }) : Conflict(new { message = "Invite not valid." }); } catch (DomainError e) { return Handle(e); } }
}
