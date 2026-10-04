using FluentValidation;

namespace HomeServices.Application.Messaging;

/// <summary>Validates a query with all its FluentValidation validators before the real handler runs.</summary>
public sealed class ValidationQueryHandlerDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAsync(query, validators, cancellationToken);
        return await inner.HandleAsync(query, cancellationToken);
    }
}
