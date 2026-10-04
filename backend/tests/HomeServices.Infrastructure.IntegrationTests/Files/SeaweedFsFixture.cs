using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HomeServices.Infrastructure.Files;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Files;

/// <summary>One SeaweedFS container (S3 API with real credentials) for the storage tests (needs Docker running).</summary>
public sealed class SeaweedFsFixture : IAsyncLifetime
{
    public const string AccessKey = "tests";
    public const string SecretKey = "tests-secret-0123456789";
    public const string Bucket = "test-files";
    private const int S3Port = 8333;

    private static readonly byte[] S3Config = Encoding.UTF8.GetBytes(
        $$"""
        {"identities":[{"name":"tests","credentials":[{"accessKey":"{{AccessKey}}","secretKey":"{{SecretKey}}"}],"actions":["Admin","Read","Write","List","Tagging"]}]}
        """);

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("chrislusf/seaweedfs:latest")
        .WithCommand("server", "-dir=/data", "-s3", $"-s3.port={S3Port}", "-s3.config=/etc/seaweedfs/s3.json")
        .WithResourceMapping(S3Config, "/etc/seaweedfs/s3.json")
        .WithPortBinding(S3Port, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
            .ForPort(S3Port)
            .ForPath("/")
            .ForStatusCodeMatching(_ => true)))
        .Build();

    public string ServiceUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(S3Port)}";

    public S3FileStorage CreateStorage(string? publicServiceUrl = null, string[]? corsOrigins = null) => new(Options.Create(new StorageSettings
    {
        ServiceUrl = ServiceUrl,
        PublicServiceUrl = publicServiceUrl,
        Bucket = Bucket,
        AccessKey = AccessKey,
        SecretKey = SecretKey,
        CreateBucket = true,
        CorsOrigins = corsOrigins ?? [],
    }));

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // The S3 port opens before the storage behind it is ready, so retry until a write works.
        using var storage = CreateStorage();
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (true)
        {
            try
            {
                await storage.EnsureReadyAsync(CancellationToken.None);
                await storage.PutAsync("ready-check", new MemoryStream([1]), "application/octet-stream", CancellationToken.None);
                return;
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class SeaweedFsCollection : ICollectionFixture<SeaweedFsFixture>
{
    public const string Name = "seaweedfs";
}
