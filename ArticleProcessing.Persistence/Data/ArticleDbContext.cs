using Microsoft.EntityFrameworkCore;

namespace ArticleProcessing.Persistence.Data;

internal class ArticleDbContext : DbContext
{
    public ArticleDbContext(DbContextOptions<ArticleDbContext> options) :
        base(options)
    {

    }
    public DbSet<ArticleProcessing.Models.Article> Articles => Set<ArticleProcessing.Models.Article>();
}
