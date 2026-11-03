using Microsoft.AspNetCore.Mvc;
using WedLink.Business.Templates;

namespace WedLink.API.Controllers;

public abstract class TemplateApiControllerBase : ControllerBase
{
    protected async Task<IActionResult> RunTemplateAction(
        Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (TemplateValidationException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "بيانات القالب غير صالحة",
                detail: exception.Message);
        }
        catch (TemplateConflictException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "تعارض في بيانات القالب",
                detail: exception.Message);
        }
        catch (TemplateNotFoundException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "القالب أو الإصدار غير موجود",
                detail: exception.Message);
        }
    }
}
