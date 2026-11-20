using ArticleProcessing.Contracts.Contracts;
using ArticleProcessing.Models;

namespace ArticleProcessing.API.Services;

public class ArticleQueueService
{
    private readonly IArticleQueue _articleQueue;

    public ArticleQueueService(IArticleQueue articleQueue)
    {
        _articleQueue = articleQueue;
    }

    public Task ProcessArticleAsync(Article article, CancellationToken cancellationToken)
    {
        return _articleQueue.EnqueueAsync(article, cancellationToken);
    }
}
