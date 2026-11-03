using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WedLink.API.Filters;

public sealed class AdminAntiforgeryFilter(IAntiforgery antiforgery)
    : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        var isReadOnly = HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || HttpMethods.IsTrace(request.Method);

        if (isReadOnly || !request.Path.StartsWithSegments("/api/v1/admin"))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "رمز الحماية غير صالح أو منتهي.",
                Detail = "حدّث الصفحة وحاول إرسال الطلب مرة أخرى."
            });
        }
    }
}
