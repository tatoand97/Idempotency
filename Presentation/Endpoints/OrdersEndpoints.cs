using PruebaIdempotencia.Application.Orders;
using PruebaIdempotencia.Domain;
using PruebaIdempotencia.Presentation.Http;
using PruebaIdempotencia.Presentation.Filters;

namespace PruebaIdempotencia.Presentation.Endpoints;

public static class OrdersEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", CreateOrder)
            .AddEndpointFilter(new IdempotencyKeyFilter())
            .WithName("CreateOrder")
            .WithOpenApi();
    }

    private static async Task<IResult> CreateOrder(
        HttpContext http,
        ICreateOrderUseCase useCase,
        OrderRequest request,
        CancellationToken ct)
    {
        var key = http.GetIdempotencyKey()!; // set by IdempotencyKeyFilter

        var contentType = http.Request.ContentType ?? string.Empty;
        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem("Unsupported Media Type. Expect application/json", statusCode: 415);
        }

        http.Response.Headers.CacheControl = "no-store";

        var result = await useCase.ExecuteAsync(request, key, ct);
        if (result.RetryAfterSeconds is { } ra)
        {
            http.Response.Headers.RetryAfter = ra.ToString();
        }
        if (result.ResponseBodyJson is { } body)
        {
            return Results.Content(body, "application/json", System.Text.Encoding.UTF8, result.StatusCode);
        }
        return Results.StatusCode(result.StatusCode);
    }
}
