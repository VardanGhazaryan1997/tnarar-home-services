using FluentValidation;

namespace HomeServices.Application.Messaging;

/// <summary>Validates a command with all its FluentValidation validators before the real handler runs.</summary>
public sealed class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        await RequestValidator.ValidateAsync(command, validators, cancellationToken);
        return await inner.HandleAsync(command, cancellationToken);
    }
}
