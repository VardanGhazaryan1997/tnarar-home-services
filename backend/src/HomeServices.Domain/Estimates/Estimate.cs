using HomeServices.Domain.Common;

namespace HomeServices.Domain.Estimates;

/// <summary>
/// A customer's renovation estimate: rooms with their sizes and the work wanted in each. Quantities come from the room
/// sizes (see <see cref="RoomGeometry"/>) unless the customer typed one; prices come from the market ranges.
/// <see cref="UserId"/> is null for an estimate made without signing in.
/// </summary>
public sealed class Estimate : AuditableEntity
{
    public const int TitleMaxLength = 120;
    public const int MaxRooms = 30;
    public const int ShareTokenMinLength = 16;
    public const int ShareTokenMaxLength = 32;

    private readonly List<EstimateRoom> _rooms = [];

    private Estimate()
    {
    }

    public Guid? UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>Where the work is (prices may differ by place later).</summary>
    public Guid? CityId { get; private set; }

    /// <summary>An old building: all work costs more (uneven walls, old pipes and wiring).</summary>
    public bool OldBuilding { get; private set; }

    /// <summary>The secret part of the estimate's share link, or null when it isn't shared.</summary>
    public string? ShareToken { get; private set; }

    public IReadOnlyCollection<EstimateRoom> Rooms => _rooms.AsReadOnly();

    public static Estimate Create(Guid? userId, string title, Guid? cityId, bool oldBuilding = false)
    {
        var estimate = new Estimate { UserId = userId, CityId = cityId, OldBuilding = oldBuilding };
        estimate.Rename(title);
        return estimate;
    }

    public void SetOldBuilding(bool oldBuilding) => OldBuilding = oldBuilding;

    /// <summary>
    /// Turns on the share link with <paramref name="token"/> (16+ URL-safe characters); an estimate already shared keeps
    /// its link. Returns the token in use.
    /// </summary>
    public string Share(string token)
    {
        if (ShareToken is not null)
        {
            return ShareToken;
        }

        if (token is not { Length: >= ShareTokenMinLength and <= ShareTokenMaxLength } || !token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new DomainException("estimate.share_token_invalid", "A share token is 16 to 32 URL-safe characters.");
        }

        ShareToken = token;
        return token;
    }

    /// <summary>Turns the share link off; the old link stops working.</summary>
    public void StopSharing() => ShareToken = null;

    /// <summary>Removes every room (to rebuild the estimate from an edited copy).</summary>
    public void ClearRooms()
    {
        foreach (var room in _rooms)
        {
            room.ClearContents();
        }

        _rooms.Clear();
    }

    public void Rename(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > TitleMaxLength)
        {
            throw new DomainException("estimate.title_invalid", $"Give the estimate a title of up to {TitleMaxLength} characters.");
        }

        Title = trimmed;
    }

    public void MoveTo(Guid? cityId) => CityId = cityId;

    /// <summary>Gives an estimate made without signing in to the user who signed in.</summary>
    public void ClaimFor(Guid userId)
    {
        if (UserId is not null && UserId != userId)
        {
            throw new DomainException("estimate.owned", "This estimate belongs to someone else.");
        }

        UserId = userId;
    }

    public EstimateRoom AddRoom(string name, RoomType type, RoomSize size, IEnumerable<RoomOpening> openings)
    {
        if (_rooms.Count >= MaxRooms)
        {
            throw new DomainException("estimate.too_many_rooms", $"An estimate has at most {MaxRooms} rooms.");
        }

        var room = new EstimateRoom(Id, _rooms.Count + 1, name, type, size, openings);
        _rooms.Add(room);
        return room;
    }

    public void RemoveRoom(Guid roomId)
    {
        var room = Room(roomId);
        _rooms.Remove(room);
        for (var i = 0; i < _rooms.Count; i++)
        {
            _rooms[i].Reorder(i + 1);
        }
    }

    public EstimateRoom Room(Guid roomId) =>
        _rooms.SingleOrDefault(r => r.Id == roomId) ?? throw new DomainException("estimate.room_not_found", "That room isn't in this estimate.");
}
