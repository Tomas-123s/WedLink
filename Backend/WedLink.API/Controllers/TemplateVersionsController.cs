using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Templates;
using WedLink.Business.Templates;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/admin/template-versions")]
public sealed class TemplateVersionsController(TemplateCatalogService catalogService)
    : TemplateApiControllerBase
{
    [HttpGet("{templateVersionId:int}")]
    public async Task<IActionResult> GetById(
        int templateVersionId,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var version = await catalogService.GetVersionAsync(
                templateVersionId, cancellationToken);
            return Ok(TemplateVersionResponse.FromData(version));
        });
    }

    [HttpPut("{templateVersionId:int}/features")]
    public async Task<IActionResult> SetFeatures(
        int templateVersionId,
        IReadOnlyList<TemplateFeatureRequest> requests,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var features = requests.Select(request => request.ToInput()).ToList();
            await catalogService.SetVersionFeaturesAsync(
                templateVersionId, features, cancellationToken);
            return NoContent();
        });
    }

    [HttpPut("{templateVersionId:int}/addons")]
    public async Task<IActionResult> SetAddOns(
        int templateVersionId,
        TemplateAddOnsRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            await catalogService.SetVersionAddOnsAsync(
                templateVersionId, request.AddOnIds, cancellationToken);
            return NoContent();
        });
    }

    [HttpPost("{templateVersionId:int}/activate")]
    public async Task<IActionResult> Activate(
        int templateVersionId,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            await catalogService.SetVersionActiveAsync(
                templateVersionId, cancellationToken);
            return NoContent();
        });
    }
}
