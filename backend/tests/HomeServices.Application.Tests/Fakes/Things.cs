using FluentValidation;
using HomeServices.Application.Messaging;

namespace HomeServices.Application.Tests.Fakes;

// Small fake use cases used to test handler registration and validation.

public sealed record CreateThing(string Name) : ICommand<Guid>;

public sealed class CreateThingHandler : ICommandHandler<CreateThing, Guid>
{
    public Task<Guid> HandleAsync(CreateThing command, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid());
}

public sealed class CreateThingValidator : AbstractValidator<CreateThing>
{
    public CreateThingValidator() => RuleFor(x => x.Name).NotEmpty().WithErrorCode("name.required");
}

public sealed record GetThing(int Id) : IQuery<string>;

public sealed class GetThingHandler : IQueryHandler<GetThing, string>
{
    public Task<string> HandleAsync(GetThing query, CancellationToken cancellationToken) => Task.FromResult($"thing-{query.Id}");
}

public sealed class GetThingValidator : AbstractValidator<GetThing>
{
    public GetThingValidator() => RuleFor(x => x.Id).GreaterThan(0).WithErrorCode("id.invalid");
}

// Must be ignored by assembly scanning: abstract and open generic handlers.
public abstract class AbstractThingHandler : ICommandHandler<CreateThing, Guid>
{
    public abstract Task<Guid> HandleAsync(CreateThing command, CancellationToken cancellationToken);
}

public class OpenGenericThingHandler<T> : IQueryHandler<GetThing, string>
{
    public Task<string> HandleAsync(GetThing query, CancellationToken cancellationToken) => Task.FromResult(typeof(T).Name);
}
