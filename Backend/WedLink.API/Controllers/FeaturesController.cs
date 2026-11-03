using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Templates;
using WedLink.Business.Templates;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/admin/features")]
public sealed class FeaturesController(TemplateCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeatureResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var features = await catalogService.GetFeaturesAsync(cancellationToken);
        return Ok(features.Select(FeatureResponse.FromData).ToList());
    }
}
