using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Content;

/// <summary>Who a question is for; the Portal shows them in separate groups.</summary>
public enum FaqAudience
{
    General = 1,
    Customers = 2,
    Partners = 3,
}

/// <summary>A frequently asked question with its answer, per language. Only published ones are shown.</summary>
public sealed class FaqItem : Entity, IAudited
{
    public const int QuestionMaxLength = 300;
    public const int AnswerMaxLength = 5_000;

    private FaqItem()
    {
    }

    public LocalizedText Question { get; private set; } = LocalizedText.Empty;

    /// <summary>Markdown per language.</summary>
    public LocalizedText Answer { get; private set; } = LocalizedText.Empty;

    public FaqAudience Audience { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsPublished { get; private set; }

    public static FaqItem Create(LocalizedText question, LocalizedText answer, FaqAudience audience, int sortOrder)
    {
        var item = new FaqItem();
        item.Update(question, answer, audience, sortOrder);
        return item;
    }

    public void Update(LocalizedText question, LocalizedText answer, FaqAudience audience, int sortOrder)
    {
        if (question.Values.Count == 0 || answer.Values.Count == 0)
        {
            throw new DomainException("faq.text_required", "A question and an answer are required.");
        }

        if (!Enum.IsDefined(audience))
        {
            throw new DomainException("faq.audience_invalid", "Unknown audience.");
        }

        Question = question;
        Answer = answer;
        Audience = audience;
        SortOrder = sortOrder;
    }

    public void Publish() => IsPublished = true;

    public void Unpublish() => IsPublished = false;
}
