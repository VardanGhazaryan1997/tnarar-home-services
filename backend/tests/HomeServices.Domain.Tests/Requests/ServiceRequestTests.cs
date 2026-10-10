using HomeServices.Domain.Catalog;
using HomeServices.Domain.Requests;

namespace HomeServices.Domain.Tests.Requests;

public class ServiceRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Customer = Guid.NewGuid();
    private static readonly Guid Plumbing = Guid.NewGuid();
    private static readonly Guid Yerevan = Guid.NewGuid();

    private static ServiceRequest NewRequest(RequestKind kind = RequestKind.Open) =>
        ServiceRequest.Create(Customer, kind, Plumbing, Yerevan, null, "  The kitchen tap is leaking badly.  ", new DateOnly(2026, 10, 7), " evenings ", 10_000, 30_000);

    [Fact]
    public void A_new_request_is_open_with_clean_details()
    {
        var request = NewRequest();

        request.Status.ShouldBe(RequestStatus.Open);
        request.Kind.ShouldBe(RequestKind.Open);
        request.CustomerId.ShouldBe(Customer);
        request.CategoryId.ShouldBe(Plumbing);
        request.CityId.ShouldBe(Yerevan);
        request.DistrictId.ShouldBeNull();
        request.Description.ShouldBe("The kitchen tap is leaking badly.");
        request.PreferredDate.ShouldBe(new DateOnly(2026, 10, 7));
        request.TimeNote.ShouldBe("evenings");
        request.BudgetMin.ShouldBe(10_000);
        request.BudgetMax.ShouldBe(30_000);
        request.NeedsAttention.ShouldBeFalse();
        request.Recipients.ShouldBeEmpty();
    }

    [Fact]
    public void Blank_time_notes_and_open_budgets_are_allowed()
    {
        var request = ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, Guid.NewGuid(), new string('a', 20), null, "  ", null, null);

        request.TimeNote.ShouldBeNull();
        request.BudgetMin.ShouldBeNull();
        request.BudgetMax.ShouldBeNull();
        request.PreferredDate.ShouldBeNull();
    }

    [Theory]
    [InlineData("too short", "request.description_length")]
    [InlineData(null, "request.description_length")]
    public void The_description_must_be_long_enough(string? description, string code) =>
        Should.Throw<DomainException>(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, null, description!, null, null, null, null))
            .Code.ShouldBe(code);

    [Fact]
    public void Details_are_validated()
    {
        static DomainException Fails(Action action) => Should.Throw<DomainException>(action);

        Fails(() => ServiceRequest.Create(Guid.Empty, RequestKind.Open, Plumbing, Yerevan, null, new string('a', 20), null, null, null, null))
            .Code.ShouldBe("request.customer_required");
        Fails(() => ServiceRequest.Create(Customer, (RequestKind)9, Plumbing, Yerevan, null, new string('a', 20), null, null, null, null))
            .Code.ShouldBe("request.kind_invalid");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Guid.Empty, Yerevan, null, new string('a', 20), null, null, null, null))
            .Code.ShouldBe("request.place_required");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Guid.Empty, null, new string('a', 20), null, null, null, null))
            .Code.ShouldBe("request.place_required");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, null, new string('a', ServiceRequest.DescriptionMaxLength + 1), null, null, null, null))
            .Code.ShouldBe("request.description_length");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, null, new string('a', 20), null, new string('n', 201), null, null))
            .Code.ShouldBe("request.time_note_too_long");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, null, new string('a', 20), null, null, 50, 10))
            .Code.ShouldBe("request.budget_invalid");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, null, new string('a', 20), null, null, -1, null))
            .Code.ShouldBe("request.budget_invalid");
        Fails(() => ServiceRequest.Create(Customer, RequestKind.Open, Plumbing, Yerevan, null, new string('a', 20), null, null, null, ServiceRequest.MaxBudget + 1))
            .Code.ShouldBe("request.budget_invalid");
    }

    [Fact]
    public void Photos_are_attached_once_in_order_up_to_the_limit()
    {
        var request = NewRequest();
        var first = Guid.NewGuid();

        request.AddMedia(first);
        request.AddMedia(first);
        for (var i = 1; i < ServiceRequest.MaxMedia; i++)
        {
            request.AddMedia(Guid.NewGuid());
        }

        request.Media.Count.ShouldBe(ServiceRequest.MaxMedia);
        request.Media.First().FileId.ShouldBe(first);
        request.Media.Select(m => m.SortOrder).ShouldBe(Enumerable.Range(1, ServiceRequest.MaxMedia));
        request.Media.ShouldAllBe(m => m.RequestId == request.Id);
        Should.Throw<DomainException>(() => request.AddMedia(Guid.NewGuid())).Code.ShouldBe("request.too_many_media");
    }

    [Fact]
    public void Sending_adds_each_partner_once_and_ends_the_wait_for_an_operator()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        var gor = Guid.NewGuid();
        request.FlagForOperator(AttentionReason.NoMatchingPartners, Now);

        request.SendTo([aram, gor, aram], RecipientSource.Matched, Now).ShouldBe(2);
        request.SendTo([aram], RecipientSource.Manual, Now).ShouldBe(0);

        request.Recipients.Select(r => r.PartnerProfileId).ShouldBe(new[] { aram, gor });
        var recipient = request.Recipients.First();
        recipient.Source.ShouldBe(RecipientSource.Matched);
        recipient.Status.ShouldBe(RecipientStatus.New);
        recipient.SentAt.ShouldBe(Now);
        recipient.RequestId.ShouldBe(request.Id);
        request.NeedsAttention.ShouldBeFalse();
        request.AttentionReason.ShouldBeNull();
    }

    [Fact]
    public void Sending_nobody_keeps_the_request_in_the_operator_queue()
    {
        var request = NewRequest();
        request.FlagForOperator(AttentionReason.NoMatchingPartners, Now);

        request.SendTo([], RecipientSource.Manual, Now.AddHours(1)).ShouldBe(0);

        request.NeedsAttentionSince.ShouldBe(Now);
        request.AttentionReason.ShouldBe(AttentionReason.NoMatchingPartners);
    }

    [Fact]
    public void A_direct_request_goes_to_one_partner_but_staff_can_add_more()
    {
        var request = NewRequest(RequestKind.Direct);

        Should.Throw<DomainException>(() => request.SendTo([Guid.NewGuid(), Guid.NewGuid()], RecipientSource.Direct, Now))
            .Code.ShouldBe("request.direct_has_partner");

        var request2 = NewRequest(RequestKind.Direct);
        request2.SendTo([Guid.NewGuid()], RecipientSource.Direct, Now);
        request2.SendTo([Guid.NewGuid()], RecipientSource.Manual, Now).ShouldBe(1);
        request2.Recipients.Count.ShouldBe(2);
    }

    [Fact]
    public void Unknown_sources_and_reasons_are_refused()
    {
        var request = NewRequest();

        Should.Throw<DomainException>(() => request.SendTo([Guid.NewGuid()], (RecipientSource)9, Now)).Code.ShouldBe("request.source_invalid");
        Should.Throw<DomainException>(() => request.FlagForOperator((AttentionReason)9, Now)).Code.ShouldBe("request.reason_invalid");
    }

    [Fact]
    public void The_first_reason_for_operator_attention_is_kept()
    {
        var request = NewRequest();

        request.FlagForOperator(AttentionReason.NoMatchingPartners, Now);
        request.FlagForOperator(AttentionReason.NoResponse, Now.AddHours(2));

        request.NeedsAttentionSince.ShouldBe(Now);
        request.AttentionReason.ShouldBe(AttentionReason.NoMatchingPartners);
    }

    [Fact]
    public void Viewing_marks_the_recipient_once()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        request.SendTo([aram], RecipientSource.Matched, Now);

        request.MarkViewed(aram, Now.AddMinutes(5));
        request.MarkViewed(aram, Now.AddMinutes(9));

        var recipient = request.FindRecipient(aram).ShouldNotBeNull();
        recipient.Status.ShouldBe(RecipientStatus.Viewed);
        recipient.ViewedAt.ShouldBe(Now.AddMinutes(5));
        request.FindRecipient(Guid.NewGuid()).ShouldBeNull();
        Should.Throw<DomainException>(() => request.MarkViewed(Guid.NewGuid(), Now)).Code.ShouldBe("request.not_recipient");
    }

    [Fact]
    public void When_the_direct_partner_declines_an_operator_takes_over()
    {
        var request = NewRequest(RequestKind.Direct);
        var aram = Guid.NewGuid();
        request.SendTo([aram], RecipientSource.Direct, Now);

        request.Decline(aram, "  Too far for me  ", Now.AddHours(1));

        var recipient = request.FindRecipient(aram)!;
        recipient.Status.ShouldBe(RecipientStatus.Declined);
        recipient.DeclineReason.ShouldBe("Too far for me");
        recipient.DeclinedAt.ShouldBe(Now.AddHours(1));
        recipient.ViewedAt.ShouldBe(Now.AddHours(1));
        request.AttentionReason.ShouldBe(AttentionReason.DirectPartnerDeclined);
        request.NeedsAttentionSince.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void An_open_request_needs_an_operator_only_when_everyone_declined()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        var gor = Guid.NewGuid();
        request.SendTo([aram, gor], RecipientSource.Matched, Now);

        request.Decline(aram, null, Now);
        request.NeedsAttention.ShouldBeFalse();
        request.FindRecipient(aram)!.DeclineReason.ShouldBeNull();

        request.Decline(gor, null, Now.AddHours(3));
        request.AttentionReason.ShouldBe(AttentionReason.AllDeclined);
    }

    [Fact]
    public void A_partner_can_decline_only_once_and_not_after_responding()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        var gor = Guid.NewGuid();
        request.SendTo([aram, gor], RecipientSource.Matched, Now);

        request.Decline(aram, null, Now);
        Should.Throw<DomainException>(() => request.Decline(aram, null, Now)).Code.ShouldBe("request.already_declined");
        Should.Throw<DomainException>(() => request.MarkResponded(aram, Now)).Code.ShouldBe("request.already_declined");

        request.MarkResponded(gor, Now);
        Should.Throw<DomainException>(() => request.Decline(gor, null, Now)).Code.ShouldBe("request.already_responded");
        Should.Throw<DomainException>(() => request.Decline(Guid.NewGuid(), null, Now)).Code.ShouldBe("request.not_recipient");
    }

    [Fact]
    public void Decline_reasons_are_limited()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        request.SendTo([aram], RecipientSource.Matched, Now);

        Should.Throw<DomainException>(() => request.Decline(aram, new string('r', RequestRecipient.DeclineReasonMaxLength + 1), Now))
            .Code.ShouldBe("request.reason_too_long");
    }

    [Fact]
    public void A_response_takes_the_request_out_of_the_operator_queue()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        request.SendTo([aram], RecipientSource.Matched, Now);
        request.FlagForOperator(AttentionReason.NoResponse, Now.AddDays(1));

        request.MarkResponded(aram, Now.AddDays(1).AddHours(1));
        request.MarkResponded(aram, Now.AddDays(2));

        var recipient = request.FindRecipient(aram)!;
        recipient.Status.ShouldBe(RecipientStatus.Responded);
        recipient.RespondedAt.ShouldBe(Now.AddDays(1).AddHours(1));
        request.NeedsAttention.ShouldBeFalse();
    }

    [Fact]
    public void Requests_nobody_answered_in_time_are_flagged_once()
    {
        var window = TimeSpan.FromHours(24);
        var request = NewRequest();
        request.SendTo([Guid.NewGuid()], RecipientSource.Matched, Now);

        request.FlagIfUnanswered(Now.AddHours(23), window).ShouldBeFalse();
        request.FlagIfUnanswered(Now.AddHours(24), window).ShouldBeTrue();
        request.FlagIfUnanswered(Now.AddHours(30), window).ShouldBeFalse();

        request.AttentionReason.ShouldBe(AttentionReason.NoResponse);
        request.NeedsAttentionSince.ShouldBe(Now.AddHours(24));
    }

    [Fact]
    public void The_response_window_restarts_when_staff_add_partners()
    {
        var window = TimeSpan.FromHours(24);
        var request = NewRequest();
        request.SendTo([Guid.NewGuid()], RecipientSource.Matched, Now);
        request.SendTo([Guid.NewGuid()], RecipientSource.Manual, Now.AddHours(20));

        request.FlagIfUnanswered(Now.AddHours(25), window).ShouldBeFalse();
        request.FlagIfUnanswered(Now.AddHours(44), window).ShouldBeTrue();
    }

    [Fact]
    public void Answered_closed_or_unsent_requests_are_not_flagged()
    {
        var window = TimeSpan.FromHours(1);
        var unsent = NewRequest();
        unsent.FlagIfUnanswered(Now.AddDays(1), window).ShouldBeFalse();

        var answered = NewRequest();
        var aram = Guid.NewGuid();
        answered.SendTo([aram], RecipientSource.Matched, Now);
        answered.MarkResponded(aram, Now);
        answered.FlagIfUnanswered(Now.AddDays(1), window).ShouldBeFalse();

        var cancelled = NewRequest();
        cancelled.SendTo([Guid.NewGuid()], RecipientSource.Matched, Now);
        cancelled.Cancel(null, Now);
        cancelled.FlagIfUnanswered(Now.AddDays(1), window).ShouldBeFalse();
    }

    [Fact]
    public void A_cancelled_request_keeps_its_reason_and_accepts_no_changes()
    {
        var request = NewRequest();
        var aram = Guid.NewGuid();
        request.SendTo([aram], RecipientSource.Matched, Now);
        request.FlagForOperator(AttentionReason.NoResponse, Now);

        request.Cancel("  Fixed it myself ", Now.AddHours(2));

        request.Status.ShouldBe(RequestStatus.Cancelled);
        request.CancelReason.ShouldBe("Fixed it myself");
        request.CancelledAt.ShouldBe(Now.AddHours(2));
        request.NeedsAttention.ShouldBeFalse();
        Should.Throw<DomainException>(() => request.Cancel(null, Now)).Code.ShouldBe("request.not_open");
        Should.Throw<DomainException>(() => request.SendTo([Guid.NewGuid()], RecipientSource.Manual, Now)).Code.ShouldBe("request.not_open");
        Should.Throw<DomainException>(() => request.Decline(aram, null, Now)).Code.ShouldBe("request.not_open");
        Should.Throw<DomainException>(() => request.MarkResponded(aram, Now)).Code.ShouldBe("request.not_open");
        Should.Throw<DomainException>(() => request.FlagForOperator(AttentionReason.NoResponse, Now)).Code.ShouldBe("request.not_open");
    }

    [Fact]
    public void Cancel_reasons_are_optional_but_limited()
    {
        var request = NewRequest();

        Should.Throw<DomainException>(() => request.Cancel(new string('r', ServiceRequest.CancelReasonMaxLength + 1), Now))
            .Code.ShouldBe("request.reason_too_long");

        request.Cancel("   ", Now);
        request.CancelReason.ShouldBeNull();
    }

    [Fact]
    public void Accepting_work_closes_the_request()
    {
        var request = NewRequest();
        request.FlagForOperator(AttentionReason.NoMatchingPartners, Now);

        request.Close(Now.AddDays(1));

        request.Status.ShouldBe(RequestStatus.Closed);
        request.ClosedAt.ShouldBe(Now.AddDays(1));
        request.NeedsAttention.ShouldBeFalse();
        Should.Throw<DomainException>(() => request.Close(Now)).Code.ShouldBe("request.not_open");
        Should.Throw<DomainException>(() => request.Cancel(null, Now)).Code.ShouldBe("request.not_open");
    }

    [Fact]
    public void A_request_made_from_an_estimate_keeps_its_lines_in_order()
    {
        var request = NewRequest();
        var estimate = Guid.NewGuid();
        var (tiling, toilet) = (Guid.NewGuid(), Guid.NewGuid());

        request.LinkEstimate(estimate);
        request.AddLine(" Bathroom ", tiling, WorkUnit.SquareMeter, 4.444m, 20_000, 36_000);
        request.AddLine("Bathroom", toilet, WorkUnit.Piece, null, null, null);

        request.EstimateId.ShouldBe(estimate);
        request.Lines.Select(l => (l.SortOrder, l.RoomName, l.WorkItemId, l.Quantity, l.EstimateMin))
            .ShouldBe(new[] { (1, "Bathroom", tiling, (decimal?)4.44m, (int?)20_000), (2, "Bathroom", toilet, (decimal?)null, (int?)null) });
    }

    [Fact]
    public void Request_lines_are_checked()
    {
        var request = NewRequest();

        Code(() => request.AddLine(" ", Guid.NewGuid(), WorkUnit.Piece, 1m, null, null)).ShouldBe("request.line_invalid");
        Code(() => request.AddLine("Hall", Guid.Empty, WorkUnit.Piece, 1m, null, null)).ShouldBe("request.line_invalid");
        Code(() => request.AddLine("Hall", Guid.NewGuid(), WorkUnit.Piece, 0m, null, null)).ShouldBe("request.line_invalid");
        Code(() => request.AddLine("Hall", Guid.NewGuid(), WorkUnit.Piece, 1m, 500, 100)).ShouldBe("request.line_invalid");
        Code(() => request.LinkEstimate(Guid.Empty)).ShouldBe("request.estimate_invalid");
        for (var i = 0; i < ServiceRequest.MaxLines; i++)
        {
            request.AddLine("Hall", Guid.NewGuid(), WorkUnit.Piece, 1m, null, null);
        }

        Code(() => request.AddLine("Hall", Guid.NewGuid(), WorkUnit.Piece, 1m, null, null)).ShouldBe("request.too_many_lines");
    }

    private static string Code(Action action) => Should.Throw<DomainException>(action).Code;
}
