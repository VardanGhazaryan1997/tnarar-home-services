using HomeServices.Api.Files;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Files;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

public class FileStorageBootstrapperTests
{
    private static ServiceProvider Services(IFileStorage storage) =>
        new ServiceCollection().AddLogging().AddSingleton(storage).BuildServiceProvider();

    [Fact]
    public async Task File_storage_is_prepared_on_startup()
    {
        var storage = new InMemoryFileStorage();
        await using var services = Services(storage);

        await services.PrepareFileStorageAsync();

        storage.EnsureReadyCalls.ShouldBe(1);
    }

    [Fact]
    public async Task The_API_starts_even_when_file_storage_is_not_running()
    {
        var storage = new InMemoryFileStorage { EnsureReadyFailure = new HttpRequestException("Connection refused") };
        await using var services = Services(storage);

        await Should.NotThrowAsync(() => services.PrepareFileStorageAsync());

        storage.EnsureReadyCalls.ShouldBe(1);
    }
}
