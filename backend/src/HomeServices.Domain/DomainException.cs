namespace HomeServices.Domain;

/// <summary>
/// A business rule was broken (e.g. completing an order that hasn't started).
/// <see cref="Code"/> is stable and translated by the frontend. The API maps it to HTTP 422.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
