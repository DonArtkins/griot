using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/invites/{token}/accept")]
[Authorize]
public class InviteController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public InviteController(IDomainService domain) { _domain = domain; }

    [HttpPost]
    public async Task<IActionResult> AcceptInvite(string token)
    { try { return (await _domain.AcceptInviteAsync(token, CurrentUserId())) ? Ok(new { message = "Invite accepted." }) : Conflict(new { message = "Invite not valid." }); } catch (DomainError e) { return Handle(e); } }
}
