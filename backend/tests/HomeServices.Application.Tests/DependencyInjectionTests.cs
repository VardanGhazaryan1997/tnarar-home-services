using FluentValidation;
using HomeServices.Application.Messaging;
using HomeServices.Application.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_returns_the_same_collection_for_chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public async Task Command_handlers_are_registered_behind_validation()
    {
        using var provider = BuildProviderWithFakes();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateThing, Guid>>();

        handler.ShouldBeOfType<ValidationCommandHandlerDecorator<CreateThing, Guid>>();
        (await handler.HandleAsync(new CreateThing("Tiles"), CancellationToken.None)).ShouldNotBe(Guid.Empty);
        await Should.ThrowAsync<ValidationException>(() => handler.HandleAsync(new CreateThing(""), CancellationToken.None));
    }

    [Fact]
    public async Task Query_handlers_are_registered_behind_validation()
    {
        using var provider = BuildProviderWithFakes();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetThing, string>>();

        handler.ShouldBeOfType<ValidationQueryHandlerDecorator<GetThing, string>>();
        (await handler.HandleAsync(new GetThing(3), CancellationToken.None)).ShouldBe("thing-3");
        await Should.ThrowAsync<ValidationException>(() => handler.HandleAsync(new GetThing(-1), CancellationToken.None));
    }

    [Fact]
    public void Abstract_and_open_generic_handlers_are_ignored()
    {
        using var provider = BuildProviderWithFakes();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<ICommandHandler<CreateThing, Guid>>().ShouldHaveSingleItem();
        scope.ServiceProvider.GetServices<IQueryHandler<GetThing, string>>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Validators_in_the_scanned_assembly_are_registered()
    {
        using var provider = BuildProviderWithFakes();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IValidator<CreateThing>>().ShouldHaveSingleItem().ShouldBeOfType<CreateThingValidator>();
    }

    private static ServiceProvider BuildProviderWithFakes() =>
        new ServiceCollection()
            .AddHandlersFromAssembly(typeof(CreateThing).Assembly)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
}
