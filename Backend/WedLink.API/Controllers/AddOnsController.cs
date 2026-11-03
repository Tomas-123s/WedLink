using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Templates;
using WedLink.Business.Templates;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/admin/addons")]
public sealed class AddOnsController(AddOnService addOnService)
    : TemplateApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AddOnResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var addOns = await addOnService.GetAddOnsAsync(cancellationToken);
        return Ok(addOns.Select(AddOnResponse.FromData).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        AddOnRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var addOn = await addOnService.CreateAddOnAsync(
                request.ToInput(), cancellationToken);
            return CreatedAtAction(
                nameof(GetAll),
                AddOnResponse.FromData(addOn));
        });
    }

    [HttpPut("{addOnId:int}")]
    public async Task<IActionResult> Update(
        int addOnId,
        AddOnRequest request,
        CancellationToken cancellationToken)
    {
        return await RunTemplateAction(async () =>
        {
            var addOn = await addOnService.UpdateAddOnAsync(
                addOnId, request.ToInput(), cancellationToken);
            return Ok(AddOnResponse.FromData(addOn));
        });
    }
}
