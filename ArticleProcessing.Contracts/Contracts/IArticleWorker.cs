using ArticleProcessing.Models;

namespace ArticleProcessing.Contracts.Contracts
{
    public interface IArticleWorker
    {
        Task ProcessAsync(Article article, CancellationToken cancellationToken);
    }
}
