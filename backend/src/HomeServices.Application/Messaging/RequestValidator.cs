using FluentValidation;
using FluentValidation.Results;

namespace HomeServices.Application.Messaging;

internal static class RequestValidator
{
    /// <summary>Runs every validator and throws one <see cref="ValidationException"/> with all failures.</summary>
    public static async Task ValidateAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
