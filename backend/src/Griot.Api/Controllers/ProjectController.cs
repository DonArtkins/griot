using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Authorize]
public class ProjectController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public ProjectController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    [Route("api/workspaces/{id}/projects")]
    public async Task<IActionResult> GetProjects(Guid id)
    { try { return Ok(await _domain.GetProjectsAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    [Route("api/workspaces/{id}/projects")]
    public async Task<IActionResult> CreateProject(Guid id, [FromBody] CreateProjectRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateProjectAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet]
    [Route("api/projects/{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    { try { return Ok(await _domain.GetProjectAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPut]
    [Route("api/projects/{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    { try { return Ok(await _domain.UpdateProjectAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete]
    [Route("api/projects/{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    { try { return (await _domain.DeleteProjectAsync(id, CurrentUserId())) ? NoContent() : NotFound(new { message = "Project not found." }); } catch (DomainError e) { return Handle(e); } }
}
