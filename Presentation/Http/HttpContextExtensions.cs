using Microsoft.AspNetCore.Http;

namespace PruebaIdempotencia.Presentation.Http;

public static class HttpContextExtensions
{
    public static string? GetIdempotencyKey(this HttpContext http)
    {
        return http.Items.TryGetValue(IdempotencyKeyFeature.ItemKey, out var value) ? value as string : null;
    }
}

