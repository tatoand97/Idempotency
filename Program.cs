using PruebaIdempotencia.Application;
using PruebaIdempotencia.Infrastructure;
using PruebaIdempotencia.Presentation;
using PruebaIdempotencia.Presentation.Endpoints;
var builder = WebApplication.CreateBuilder(args);

// Composition root delegates DI to extensions per layer
builder.Services.AddPresentation(builder.Configuration)
                .AddApplication(builder.Configuration)
                .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Ensure database exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Map endpoints
OrdersEndpoints.Map(app);

app.Run();
