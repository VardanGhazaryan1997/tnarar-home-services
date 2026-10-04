using FluentValidation;
using HomeServices.Application.Messaging;
using HomeServices.Application.Tests.Fakes;
using NSubstitute;

namespace HomeServices.Application.Tests.Messaging;

public class ValidationDecoratorTests
{
    [Fact]
    public async Task Command_with_validation_errors_throws_and_does_not_reach_the_handler()
    {
        var inner = Substitute.For<ICommandHandler<CreateThing, Guid>>();
        var sut = new ValidationCommandHandlerDecorator<CreateThing, Guid>(inner, [new CreateThingValidator()]);

        var exception = await Should.ThrowAsync<ValidationException>(() => sut.HandleAsync(new CreateThing(""), CancellationToken.None));

        exception.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("name.required");
        await inner.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
    }

    [Fact]
    public async Task Valid_command_is_passed_to_the_handler()
    {
        var expected = Guid.NewGuid();
        var command = new CreateThing("Plumbing");
        var inner = Substitute.For<ICommandHandler<CreateThing, Guid>>();
        inner.HandleAsync(command, Arg.Any<CancellationToken>()).Returns(expected);
        var sut = new ValidationCommandHandlerDecorator<CreateThing, Guid>(inner, [new CreateThingValidator()]);

        var result = await sut.HandleAsync(command, CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task Command_without_validators_is_passed_to_the_handler()
    {
        var inner = Substitute.For<ICommandHandler<CreateThing, Guid>>();
        var sut = new ValidationCommandHandlerDecorator<CreateThing, Guid>(inner, []);

        await sut.HandleAsync(new CreateThing(""), CancellationToken.None);

        await inner.Received(1).HandleAsync(Arg.Any<CreateThing>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Errors_from_all_validators_are_reported_together()
    {
        var inner = Substitute.For<ICommandHandler<CreateThing, Guid>>();
        var sut = new ValidationCommandHandlerDecorator<CreateThing, Guid>(inner, [new CreateThingValidator(), new CreateThingValidator()]);

        var exception = await Should.ThrowAsync<ValidationException>(() => sut.HandleAsync(new CreateThing(""), CancellationToken.None));

        exception.Errors.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Query_with_validation_errors_throws_and_does_not_reach_the_handler()
    {
        var inner = Substitute.For<IQueryHandler<GetThing, string>>();
        var sut = new ValidationQueryHandlerDecorator<GetThing, string>(inner, [new GetThingValidator()]);

        var exception = await Should.ThrowAsync<ValidationException>(() => sut.HandleAsync(new GetThing(0), CancellationToken.None));

        exception.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("id.invalid");
        await inner.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
    }

    [Fact]
    public async Task Valid_query_is_passed_to_the_handler()
    {
        var query = new GetThing(7);
        var inner = Substitute.For<IQueryHandler<GetThing, string>>();
        inner.HandleAsync(query, Arg.Any<CancellationToken>()).Returns("thing-7");
        var sut = new ValidationQueryHandlerDecorator<GetThing, string>(inner, [new GetThingValidator()]);

        var result = await sut.HandleAsync(query, CancellationToken.None);

        result.ShouldBe("thing-7");
    }
}
