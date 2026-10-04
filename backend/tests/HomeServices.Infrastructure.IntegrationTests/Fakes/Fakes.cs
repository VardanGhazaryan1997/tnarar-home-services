using HomeServices.Application.Abstractions;
using HomeServices.Application.Files;
using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Fakes;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class FakeCurrentLanguage(string code = "hy") : ICurrentLanguage
{
    public string Code { get; } = code;

    public string DefaultCode => "hy";
}

/// <summary>Signed download links that need no storage.</summary>
public sealed class FakeFileStorage : IFileStorage
{
    public Task<Uri> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.test/{key}?upload"));

    public Task<Uri> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.test/{key}"));

    public Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task DeleteAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task EnsureReadyAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; }

    public bool IsStaff { get; set; }
}

/// <summary>A test-only entity: auditable, soft-deletable and in the audit log, with a domain event.</summary>
public sealed class Widget : SoftDeletableEntity, IAudited
{
    public required string DisplayName { get; set; }

    public LocalizedText Title { get; set; } = LocalizedText.Empty;

    [NotAudited]
    public int Views { get; set; }

    [AuditRedacted]
    public string? Secret { get; set; }

    public void Rename(string name)
    {
        DisplayName = name;
        RaiseDomainEvent(new WidgetRenamed(Id));
    }
}

public sealed record WidgetRenamed(Guid WidgetId) : IDomainEvent;

/// <summary>A test-only entity that is not in the audit log.</summary>
public sealed class Gadget : Entity
{
    public required string DisplayName { get; set; }
}

/// <summary>The real AppDbContext plus a test entity, so conventions and auditing can be checked.</summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : AppDbContext(options)
{
    public DbSet<Widget> Widgets => Set<Widget>();

    public DbSet<Gadget> Gadgets => Set<Gadget>();
}
