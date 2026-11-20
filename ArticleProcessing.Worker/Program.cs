using AricleProcessing.Queue.Queue;
using ArticleProcessing.Contracts.Contracts;
using ArticleProcessing.Persistence;
using ArticleProcessing.Worker;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ArticleDb")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:ArticleDb for the Worker.");
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:RabbitMq for the Worker.");
builder.Services.AddArticlePersistence(connectionString);
builder.Services.AddSingleton<IArticleQueue>(_ => new RabbitMqArticleQueue(rabbitMqConnection));
builder.Services.AddHostedService<ArticleWorker>();

var host = builder.Build();
host.Run();
