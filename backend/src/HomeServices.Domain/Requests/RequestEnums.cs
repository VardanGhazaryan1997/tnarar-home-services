namespace HomeServices.Domain.Requests;

public enum RequestKind
{
    /// <summary>"Call this specialist": sent to the one partner the customer chose.</summary>
    Direct = 1,

    /// <summary>"Receive offers": sent to approved partners who offer the category in the area.</summary>
    Open = 2,
}

public enum RequestStatus
{
    /// <summary>Partners can see it and reply.</summary>
    Open = 1,

    /// <summary>The customer or staff withdrew it.</summary>
    Cancelled = 2,

    /// <summary>An offer was accepted and became an order (offers arrive in T45).</summary>
    Closed = 3,
}

/// <summary>How a partner came to receive a request.</summary>
public enum RecipientSource
{
    /// <summary>The customer chose this partner.</summary>
    Direct = 1,

    /// <summary>The partner offers the category in the area.</summary>
    Matched = 2,

    /// <summary>A staff member added the partner.</summary>
    Manual = 3,
}

public enum RecipientStatus
{
    New = 1,
    Viewed = 2,
    Declined = 3,

    /// <summary>The partner asked a question, proposed a visit or sent an offer.</summary>
    Responded = 4,
}

/// <summary>Why an open request is waiting for an operator in the Back Office.</summary>
public enum AttentionReason
{
    /// <summary>No approved partner offers the category in the area.</summary>
    NoMatchingPartners = 1,

    /// <summary>Nobody responded within the response window.</summary>
    NoResponse = 2,

    /// <summary>The partner of a direct request declined it.</summary>
    DirectPartnerDeclined = 3,

    /// <summary>Every partner who received it declined.</summary>
    AllDeclined = 4,
}
