using HomeServices.Application.Abstractions;
using HomeServices.Application.Files;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Requests;

/// <summary>The partner test catalog plus helpers for requests: handlers wired to it, partners and requests.</summary>
public sealed class RequestTestData
{
    private int _created;

    public PartnerTestData Partners { get; } = new();

    public InMemoryAppDbContext Db => Partners.Db;

    public FakeClock Clock => Partners.Clock;

    public static DateTimeOffset Now => PartnerTestData.Now;

    public RequestSettings Settings { get; } = new();

    public ICurrentLanguage Language { get; } = new FakeCurrentLanguage("hy");

    public FileDtoFactory Files => new(Partners.Storage, Options.Create(new FileSettings()), Clock);

    /// <summary>The signed-in customer (<see cref="PartnerTestData.User"/>).</summary>
    public ICurrentUser Customer => Partners.Me;

    public static ICurrentUser Staff => new FakeCurrentUser(Guid.NewGuid(), isStaff: true);

    public static ICurrentUser As(PartnerProfile partner) => new FakeCurrentUser(partner.UserId);

    /// <summary>An approved partner offering plumbing in all of Yerevan, unless configured otherwise.</summary>
    public PartnerProfile GivenPartner(string name, Action<PartnerProfile>? configure = null, PartnerStatus status = PartnerStatus.Approved) =>
        Partners.GivenProfile(name, status, configure: profile =>
        {
            profile.SetServices([Partners.Plumbing.Id]);
            profile.SetAreas([(Partners.Yerevan.Id, null)]);
            configure?.Invoke(profile);
        });

    public CreateRequest OpenRequest(Guid? categoryId = null, Guid? districtId = null) => new(
        RequestKind.Open,
        null,
        categoryId ?? Partners.Plumbing.Id,
        Partners.Yerevan.Id,
        districtId,
        "The kitchen tap is leaking, please help.",
        new DateOnly(2026, 10, 7),
        "evenings",
        10_000,
        30_000,
        null);

    public CreateRequest DirectRequest(Guid partnerId) => OpenRequest() with { Kind = RequestKind.Direct, PartnerId = partnerId };

    public Task<MyRequestDto> CreateAsync(CreateRequest command, ICurrentUser? user = null) =>
        new CreateRequestHandler(Db, user ?? Customer, Language, Files, Options.Create(Settings), Clock).HandleAsync(command, CancellationToken.None);

    /// <summary>A request stored directly, sent to <paramref name="partners"/>.</summary>
    public ServiceRequest GivenRequest(RequestKind kind = RequestKind.Open, Guid? customerId = null, DateTimeOffset? sentAt = null, string description = "Need a plumber for the bathroom.", params PartnerProfile[] partners)
    {
        var request = ServiceRequest.Create(customerId ?? Partners.User.Id, kind, Partners.Plumbing.Id, Partners.Yerevan.Id, null, description, null, null, null, null);
        request.MarkCreated(Now.AddMinutes(_created++), null);
        if (partners.Length > 0)
        {
            request.SendTo(partners.Select(p => p.Id), kind == RequestKind.Direct ? RecipientSource.Direct : RecipientSource.Matched, sentAt ?? Now);
        }

        Db.ServiceRequests.Add(request);
        Db.SaveChanges();
        return request;
    }
}
