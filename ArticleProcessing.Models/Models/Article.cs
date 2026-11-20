namespace ArticleProcessing.Models;

public class Article
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public ArticleStatus Status { get; set; }

}

public enum ArticleStatus
{
    Pending,
    Processing,
    Published,
    Failed
}