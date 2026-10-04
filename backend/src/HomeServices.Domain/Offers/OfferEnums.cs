namespace HomeServices.Domain.Offers;

/// <summary><see cref="Work"/>: the job itself. <see cref="Visit"/>: an assessment visit (possibly free) before a work offer.</summary>
public enum OfferKind
{
    Work = 1,
    Visit = 2,
}

/// <summary>
/// <see cref="Sent"/>: waiting for the customer (until it expires). <see cref="Closed"/>: the request was closed
/// (another offer accepted, or the request cancelled) while this one waited.
/// </summary>
public enum OfferStatus
{
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Withdrawn = 4,
    Expired = 5,
    Closed = 6,
}

/// <summary>What a payment stage is for.</summary>
public enum PaymentPurpose
{
    Deposit = 1,
    Stage = 2,
    Final = 3,
}
