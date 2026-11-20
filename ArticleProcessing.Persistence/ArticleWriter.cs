using ArticleProcessing.Models;
using ArticleProcessing.Persistence.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArticleProcessing.Persistence;

public sealed class ArticleWriter
{
    private readonly ArticleDbContext _dbContext;

    internal ArticleWriter(ArticleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(Article article, CancellationToken cancellationToken)
    {
        _dbContext.Articles.Add(article);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _dbContext.Entry(article).State = EntityState.Detached;
            if (!await _dbContext.Articles.AnyAsync(existing => existing.Id == article.Id, cancellationToken))
            {
                throw;
            }
        }
    }
}
