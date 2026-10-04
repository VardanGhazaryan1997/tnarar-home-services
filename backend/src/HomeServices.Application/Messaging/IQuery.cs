namespace HomeServices.Application.Messaging;

/// <summary>A read-only request that returns <typeparamref name="TResult"/>.</summary>
public interface IQuery<TResult>;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
