using System.Reflection;
using HomeServices.Application;
using HomeServices.Domain;
using HomeServices.Infrastructure;
using NetArchTest.Rules;

namespace HomeServices.Architecture.Tests;

/// <summary>
/// Clean Architecture rule: dependencies point inward only.
/// Domain ← Application ← Infrastructure / Api.
/// </summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "HomeServices.Domain";
    private const string ApplicationNamespace = "HomeServices.Application";
    private const string InfrastructureNamespace = "HomeServices.Infrastructure";
    private const string ApiNamespace = "HomeServices.Api";

    private static readonly Assembly DomainAssembly = typeof(DomainAssemblyReference).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationAssemblyReference).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InfrastructureAssemblyReference).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_other_layers_or_frameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    // Application may use EF Core's abstractions (DbSet, LINQ queries) through IAppDbContext,
    // but never the PostgreSQL provider, Infrastructure, or the web framework.
    [Fact]
    public void Application_does_not_depend_on_Infrastructure_Api_the_database_provider_or_the_web_framework()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace,
                "Npgsql",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Each_layer_lives_in_its_own_namespace()
    {
        typeof(DomainAssemblyReference).Namespace.ShouldBe(DomainNamespace);
        typeof(ApplicationAssemblyReference).Namespace.ShouldBe(ApplicationNamespace);
        typeof(InfrastructureAssemblyReference).Namespace.ShouldBe(InfrastructureNamespace);
    }

    private static string FailingTypes(TestResult result) =>
        "Types breaking the rule: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
