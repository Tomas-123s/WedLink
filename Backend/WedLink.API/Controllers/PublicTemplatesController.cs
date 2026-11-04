using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Templates;
using WedLink.Business.Templates;

namespace WedLink.API.Controllers;

[ApiController, AllowAnonymous, Route("api/v1/public/templates")]
public sealed class PublicTemplatesController(TemplateService templates) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicTemplateResponse>>> Get(CancellationToken cancellationToken) =>
        Ok((await templates.GetPublicTemplatesAsync(cancellationToken)).Select(PublicTemplateResponse.FromData).ToArray());
}
