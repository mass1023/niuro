using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/customers", (JsonElement payload, ILogger<Program> logger) =>
{
    logger.LogInformation("POST /customers -> created. Payload: {Payload}", payload.GetRawText());
    return Results.Ok();
});

app.MapPut("/customers/{id:guid}", (Guid id, JsonElement payload, ILogger<Program> logger) =>
{
    logger.LogInformation("PUT /customers/{CustomerId} -> updated. Payload: {Payload}", id, payload.GetRawText());
    return Results.Ok();
});

app.Run();
