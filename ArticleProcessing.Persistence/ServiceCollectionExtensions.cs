using ArticleProcessing.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ArticleProcessing.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArticlePersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ArticleDbContext>(options => options.UseSqlServer(connectionString, sql =>
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));
        services.AddScoped(sp => new ArticleWriter(sp.GetRequiredService<ArticleDbContext>()));
        return services;
    }
}
