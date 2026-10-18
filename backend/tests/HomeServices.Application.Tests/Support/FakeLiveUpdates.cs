using HomeServices.Application.Realtime;

namespace HomeServices.Application.Tests.Support;

/// <summary>Records the live updates handlers push, and who they went to.</summary>
public sealed class FakeLiveUpdates : ILiveUpdates
{
    public List<(IReadOnlyCollection<Guid> Users, RequestUpdateDto Update)> Requests { get; } = [];

    public List<(IReadOnlyCollection<Guid> Users, OfferUpdateDto Update)> Offers { get; } = [];

    public Task RequestUpdatedAsync(IReadOnlyCollection<Guid> userIds, RequestUpdateDto update, CancellationToken cancellationToken)
    {
        Requests.Add((userIds, update));
        return Task.CompletedTask;
    }

    public Task OfferUpdatedAsync(IReadOnlyCollection<Guid> userIds, OfferUpdateDto update, CancellationToken cancellationToken)
    {
        Offers.Add((userIds, update));
        return Task.CompletedTask;
    }
}
