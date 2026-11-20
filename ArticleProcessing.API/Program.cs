using AricleProcessing.Queue.Queue;
using ArticleProcessing.API.Services;
using ArticleProcessing.Contracts.Contracts;
using ArticleProcessing.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:RabbitMq for the API.");
builder.Services.AddSingleton<IArticleQueue>(_ => new RabbitMqArticleQueue(rabbitMqConnection));
builder.Services.AddSingleton<ArticleQueueService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/articles", async (Article article, ArticleQueueService queueService, HttpRequest request, CancellationToken cancellationToken) =>
{
    if (request.Headers.TryGetValue("Idempotency-Key", out var keys))
    {
        if (keys.Count != 1 || !Guid.TryParseExact(keys[0], "D", out var key) || key == Guid.Empty)
        {
            return Results.Text("Idempotency-Key must be a single non-empty UUID.", statusCode: StatusCodes.Status400BadRequest);
        }

        article.Id = key;
    }
    else
    {
        article.Id = Guid.CreateVersion7();
    }

    article.CreatedOn = DateTime.UtcNow;

    await queueService.ProcessArticleAsync(article, cancellationToken);

    return Results.Accepted(value: article);
});

app.Run();

[JsonSerializable(typeof(Article))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{ }
