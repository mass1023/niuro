using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var customers = new ConcurrentDictionary<Guid, JsonElement>();

app.MapPut("/customers/{id:guid}", (Guid id, JsonElement payload, ILogger<Program> logger) =>
{
    var created = customers.TryAdd(id, payload);
    if (!created) customers[id] = payload;

    logger.LogInformation("PUT /customers/{CustomerId} -> {Result}. Payload: {Payload}",
        id, created ? "created" : "updated", payload.GetRawText());
    return Results.Ok();
});

app.Run();
