using Microsoft.AspNetCore.Http;
using PruebaIdempotencia.Presentation.Http;

namespace PruebaIdempotencia.Presentation.Filters;

public sealed class IdempotencyKeyFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!http.Request.Headers.TryGetValue(HttpHeaderNames.IdempotencyKey, out var values))
        {
            return ValueTask.FromResult<object?>(Results.Problem("Missing Idempotency-Key header", statusCode: 400));
        }

        var key = values.ToString().Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return ValueTask.FromResult<object?>(Results.Problem("Empty Idempotency-Key header", statusCode: 400));
        }

        http.Items[IdempotencyKeyFeature.ItemKey] = key;
        return next(context);
    }
}

