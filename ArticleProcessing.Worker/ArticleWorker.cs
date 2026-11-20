using ArticleProcessing.Contracts.Contracts;
using ArticleProcessing.Models;
using ArticleProcessing.Persistence;

namespace ArticleProcessing.Worker;

public class ArticleWorker(IArticleQueue articleQueue, IServiceScopeFactory scopeFactory, ILogger<ArticleWorker> logger) : BackgroundService, IArticleWorker
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var delivery = await articleQueue.DequeueAsync(stoppingToken);
                try
                {
                    await ProcessAsync(delivery.Article, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Could not save article {ArticleId}. Rejecting delivery.", delivery.Article.Id);
                    await delivery.RejectAsync(stoppingToken);
                    continue;
                }

                await delivery.AcknowledgeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Article queue operation failed. Retrying shortly.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    public async Task ProcessAsync(Article article, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<ArticleWriter>();
        await writer.SaveAsync(article, cancellationToken);
    }
}
