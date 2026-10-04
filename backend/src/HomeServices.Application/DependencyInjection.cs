using System.Reflection;
using FluentValidation;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Application.Staff;
using HomeServices.Application.Translations;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StaffTwoFactorSteps>();
        services.AddScoped<SuperAdminGuard>();
        services.AddScoped<FileDtoFactory>();
        services.AddScoped<PartnerProfileDtoFactory>();
        services.AddSingleton<UiTextsVersion>();
        return services.AddHandlersFromAssembly(typeof(DependencyInjection).Assembly);
    }

    /// <summary>
    /// Registers every concrete command/query handler in <paramref name="assembly"/>, each wrapped
    /// in a validation decorator, plus every FluentValidation validator.
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var handlerTypes = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false });
        foreach (var handlerType in handlerTypes)
        {
            foreach (var contract in handlerType.GetInterfaces().Where(i => i.IsGenericType))
            {
                var decorator = DecoratorFor(contract.GetGenericTypeDefinition());
                if (decorator is null)
                {
                    continue;
                }

                var decoratorType = decorator.MakeGenericType(contract.GetGenericArguments());
                services.AddScoped(handlerType);
                services.AddScoped(contract, sp =>
                    ActivatorUtilities.CreateInstance(sp, decoratorType, sp.GetRequiredService(handlerType)));
            }
        }

        return services;
    }

    private static Type? DecoratorFor(Type contractDefinition) =>
        contractDefinition == typeof(ICommandHandler<,>) ? typeof(ValidationCommandHandlerDecorator<,>)
        : contractDefinition == typeof(IQueryHandler<,>) ? typeof(ValidationQueryHandlerDecorator<,>)
        : null;
}
