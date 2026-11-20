using ArticleProcessing.Models;

namespace ArticleProcessing.Contracts.Contracts;

public interface IArticleQueue
{
    Task EnqueueAsync(Article article, CancellationToken cancellationToken = default);
    Task<IArticleDelivery> DequeueAsync(CancellationToken cancellationToken = default);
}

public interface IArticleDelivery : IAsyncDisposable
{
    Article Article { get; }
    Task AcknowledgeAsync(CancellationToken cancellationToken = default);
    Task RejectAsync(CancellationToken cancellationToken = default);
}
