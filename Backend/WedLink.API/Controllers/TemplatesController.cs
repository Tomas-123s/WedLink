using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Templates;
using WedLink.Business.Templates;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/admin/templates")]
public sealed class TemplatesController(TemplateCatalogService catalogService)
    : TemplateApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await RunTemplateAction(async () =>
        {
            var result = await catalogService.SearchTemplatesAsync(
                search, page, pageSize, cancellationToken);
            return Ok(TemplatePageResponse.FromData(result));
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        TemplateRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var template = await catalogService.CreateTemplateAsync(
                request.ToInput(), cancellationToken);
            var response = TemplateResponse.FromData(template);
            return CreatedAtAction(
                nameof(GetById),
                new { templateId = response.TemplateId },
                response);
        });
    }

    [HttpGet("{templateId:int}")]
    public async Task<IActionResult> GetById(
        int templateId,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var template = await catalogService.GetTemplateAsync(
                templateId, cancellationToken);
            return Ok(TemplateResponse.FromData(template));
        });
    }

    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> Update(
        int templateId,
        TemplateRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var template = await catalogService.UpdateTemplateAsync(
                templateId, request.ToInput(), cancellationToken);
            return Ok(TemplateResponse.FromData(template));
        });
    }

    [HttpPatch("{templateId:int}/status")]
    public async Task<IActionResult> SetStatus(
        int templateId,
        TemplateStatusRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            await catalogService.SetTemplateActiveAsync(
                templateId, request.IsActive, cancellationToken);
            return NoContent();
        });
    }

    [HttpPost("{templateId:int}/versions")]
    public async Task<IActionResult> CreateVersion(
        int templateId,
        TemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var version = await catalogService.CreateVersionAsync(
                templateId, request.ToInput(), cancellationToken);
            return CreatedAtAction(
                nameof(TemplateVersionsController.GetById),
                "TemplateVersions",
                new { templateVersionId = version.TemplateVersionId },
                TemplateVersionResponse.FromData(version));
        });
    }
}
