using System.Text.Json;
using System.Text.Json.Serialization;
using ArticleProcessing.Contracts.Contracts;
using ArticleProcessing.Models;
using RabbitMQ.Client;

namespace AricleProcessing.Queue.Queue;

public sealed class RabbitMqArticleQueue(string connectionString) : IArticleQueue, IAsyncDisposable
{
    private const string QueueName = "articles";
    private const string FailedQueueName = "articles.failed";
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromMilliseconds(250);
    private readonly ConnectionFactory _factory = new() { Uri = new Uri(connectionString), AutomaticRecoveryEnabled = false };
    private readonly SemaphoreSlim _connectionLock = new(1);
    private IConnection? _connection;

    public async Task EnqueueAsync(Article article, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var options = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(options, cancellationToken);
        var body = JsonSerializer.SerializeToUtf8Bytes(article, QueueJsonContext.Default.Article);
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json", MessageId = article.Id.ToString() };
        await channel.BasicPublishAsync("", QueueName, mandatory: true, properties, body, cancellationToken);
    }

    public async Task<IArticleDelivery> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        try
        {
            while (true)
            {
                var message = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken);
                if (message is null)
                {
                    await Task.Delay(EmptyQueueDelay, cancellationToken);
                    continue;
                }

                Article? article;
                try
                {
                    article = JsonSerializer.Deserialize(message.Body.Span, QueueJsonContext.Default.Article);
                }
                catch (JsonException)
                {
                    await channel.BasicRejectAsync(message.DeliveryTag, requeue: false, cancellationToken);
                    continue;
                }

                if (article is null)
                {
                    await channel.BasicRejectAsync(message.DeliveryTag, requeue: false, cancellationToken);
                    continue;
                }

                return new Delivery(article, channel, message.DeliveryTag);
            }
        }
        catch
        {
            await channel.DisposeAsync();
            throw;
        }
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var connection = await _factory.CreateConnectionAsync(cancellationToken);
            try
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await channel.QueueDeclareAsync(FailedQueueName, durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?>
                    {
                        ["x-dead-letter-exchange"] = "",
                        ["x-dead-letter-routing-key"] = FailedQueueName
                    }, cancellationToken: cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }

            _connection = connection;
            return connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }

    private sealed class Delivery(Article article, IChannel channel, ulong tag) : IArticleDelivery
    {
        public Article Article { get; } = article;

        public async Task AcknowledgeAsync(CancellationToken cancellationToken = default)
            => await channel.BasicAckAsync(tag, multiple: false, cancellationToken);

        public async Task RejectAsync(CancellationToken cancellationToken = default)
            => await channel.BasicRejectAsync(tag, requeue: false, cancellationToken);

        public ValueTask DisposeAsync() => channel.DisposeAsync();
    }
}

[JsonSerializable(typeof(Article))]
internal partial class QueueJsonContext : JsonSerializerContext
{
}
