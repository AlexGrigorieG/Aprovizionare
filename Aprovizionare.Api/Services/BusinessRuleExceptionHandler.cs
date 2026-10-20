using Microsoft.AspNetCore.Diagnostics;

namespace Aprovizionare.Api.Services;

public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { message = exception.Message }, cancellationToken);
        return true;
    }
}
