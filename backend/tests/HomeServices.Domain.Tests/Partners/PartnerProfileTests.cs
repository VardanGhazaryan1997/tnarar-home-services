using HomeServices.Domain.Common;
using HomeServices.Domain.Partners;

namespace HomeServices.Domain.Tests.Partners;

public class PartnerProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid Plumbing = Guid.CreateVersion7();
    private static readonly Guid Heating = Guid.CreateVersion7();
    private static readonly Guid Yerevan = Guid.CreateVersion7();
    private static readonly Guid Kentron = Guid.CreateVersion7();
    private static readonly Guid Arabkir = Guid.CreateVersion7();
    private static readonly Guid Masis = Guid.CreateVersion7();
    private static readonly string LongAbout = new('a', PartnerProfile.AboutMinLengthToSubmit);

    private static PartnerProfile Draft() => PartnerProfile.Create(UserId, PartnerType.Specialist, "Aram Plumbing");

    private static PartnerProfile Complete()
    {
        var profile = Draft();
        profile.UpdateDetails(PartnerType.Specialist, "Aram Plumbing", LongAbout, 12, null);
        profile.SetServices([Plumbing]);
        profile.SetAreas([(Yerevan, null)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, Guid.CreateVersion7(), "Bathroom");
        return profile;
    }

    private static PartnerProfile UnderReview()
    {
        var profile = Complete();
        profile.Submit(Now);
        return profile;
    }

    private static PartnerProfile Approved()
    {
        var profile = UnderReview();
        profile.Approve(Now);
        return profile;
    }

    [Fact]
    public void A_new_profile_is_an_editable_draft()
    {
        var profile = PartnerProfile.Create(UserId, PartnerType.Company, "  Best Build LLC ");

        profile.UserId.ShouldBe(UserId);
        profile.Type.ShouldBe(PartnerType.Company);
        profile.DisplayName.ShouldBe("Best Build LLC");
        profile.Status.ShouldBe(PartnerStatus.Draft);
        profile.About.ShouldBeEmpty();
        profile.CanEdit.ShouldBeTrue();
        profile.CanSubmit.ShouldBeFalse();
        profile.StatusChanges.ShouldBeEmpty();
        profile.ReviewComment.ShouldBeNull();
    }

    [Fact]
    public void A_profile_needs_a_user_a_known_type_and_a_sensible_name()
    {
        Should.Throw<DomainException>(() => PartnerProfile.Create(Guid.Empty, PartnerType.Specialist, "Aram")).Code.ShouldBe("partner.user_required");
        Should.Throw<DomainException>(() => PartnerProfile.Create(UserId, (PartnerType)9, "Aram")).Code.ShouldBe("partner.type_invalid");
        Should.Throw<DomainException>(() => PartnerProfile.Create(UserId, PartnerType.Specialist, " A ")).Code.ShouldBe("partner.display_name_invalid");
        Should.Throw<DomainException>(() => PartnerProfile.Create(UserId, PartnerType.Specialist, new string('a', 101))).Code.ShouldBe("partner.display_name_invalid");
        Should.Throw<DomainException>(() => PartnerProfile.Create(UserId, PartnerType.Specialist, null!)).Code.ShouldBe("partner.display_name_invalid");
    }

    [Fact]
    public void Details_are_updated_and_trimmed()
    {
        var profile = Draft();
        var avatar = Guid.CreateVersion7();

        profile.UpdateDetails(PartnerType.Company, " Aram & Sons ", "  We fix pipes.  ", 15, avatar);

        profile.Type.ShouldBe(PartnerType.Company);
        profile.DisplayName.ShouldBe("Aram & Sons");
        profile.About.ShouldBe("We fix pipes.");
        profile.YearsOfExperience.ShouldBe(15);
        profile.AvatarFileId.ShouldBe(avatar);

        profile.UpdateDetails(PartnerType.Company, "Aram & Sons", null, null, null);
        profile.About.ShouldBeEmpty();
        profile.YearsOfExperience.ShouldBeNull();
    }

    [Fact]
    public void Details_have_limits()
    {
        var profile = Draft();

        Should.Throw<DomainException>(() => profile.UpdateDetails(PartnerType.Specialist, "Aram", new string('a', PartnerProfile.AboutMaxLength + 1), null, null))
            .Code.ShouldBe("partner.about_too_long");
        Should.Throw<DomainException>(() => profile.UpdateDetails(PartnerType.Specialist, "Aram", null, -1, null)).Code.ShouldBe("partner.years_invalid");
        Should.Throw<DomainException>(() => profile.UpdateDetails(PartnerType.Specialist, "Aram", null, 71, null)).Code.ShouldBe("partner.years_invalid");
    }

    [Fact]
    public void Services_are_replaced_without_duplicates()
    {
        var profile = Draft();
        profile.SetServices([Plumbing, Plumbing]);
        var kept = profile.Services.Single();

        profile.SetServices([Heating, Plumbing]);

        profile.Services.Select(s => s.CategoryId).ShouldBe(new[] { Plumbing, Heating }, ignoreOrder: true);
        profile.Services.ShouldContain(kept);
        profile.Services.ShouldAllBe(s => s.PartnerProfileId == profile.Id);

        profile.SetServices([Heating]);
        profile.Services.ShouldHaveSingleItem().CategoryId.ShouldBe(Heating);
    }

    [Fact]
    public void Services_have_a_limit()
    {
        var profile = Draft();
        var tooMany = Enumerable.Range(0, PartnerProfile.MaxServices + 1).Select(_ => Guid.CreateVersion7());

        Should.Throw<DomainException>(() => profile.SetServices(tooMany)).Code.ShouldBe("partner.too_many_services");
    }

    [Fact]
    public void A_whole_city_covers_its_districts()
    {
        var profile = Draft();

        profile.SetAreas([(Yerevan, Kentron), (Yerevan, null), (Masis, null), (Masis, null)]);

        profile.Areas.Select(a => (a.CityId, a.DistrictId)).ShouldBe(new (Guid, Guid?)[] { (Yerevan, null), (Masis, null) }, ignoreOrder: true);
        profile.Areas.ShouldAllBe(a => a.PartnerProfileId == profile.Id);
    }

    [Fact]
    public void Areas_are_replaced()
    {
        var profile = Draft();
        profile.SetAreas([(Yerevan, Kentron), (Yerevan, Arabkir)]);
        var kentron = profile.Areas.Single(a => a.DistrictId == Kentron);

        profile.SetAreas([(Yerevan, Kentron), (Masis, null)]);

        profile.Areas.Select(a => (a.CityId, a.DistrictId)).ShouldBe(new (Guid, Guid?)[] { (Yerevan, Kentron), (Masis, null) }, ignoreOrder: true);
        profile.Areas.ShouldContain(kentron);
    }

    [Fact]
    public void Areas_have_a_limit()
    {
        var profile = Draft();
        var tooMany = Enumerable.Range(0, PartnerProfile.MaxAreas + 1).Select(_ => (Guid.CreateVersion7(), (Guid?)null));

        Should.Throw<DomainException>(() => profile.SetAreas(tooMany)).Code.ShouldBe("partner.too_many_areas");
    }

    [Fact]
    public void Media_are_numbered_per_kind()
    {
        var profile = Draft();

        var first = profile.AddMedia(PartnerMediaKind.WorkExample, Guid.CreateVersion7(), "  Kitchen ");
        var second = profile.AddMedia(PartnerMediaKind.WorkExample, Guid.CreateVersion7(), " ");
        var document = profile.AddMedia(PartnerMediaKind.Document, Guid.CreateVersion7(), null);

        first.SortOrder.ShouldBe(1);
        first.Caption.ShouldBe("Kitchen");
        first.PartnerProfileId.ShouldBe(profile.Id);
        second.SortOrder.ShouldBe(2);
        second.Caption.ShouldBeNull();
        document.SortOrder.ShouldBe(1);
        document.Kind.ShouldBe(PartnerMediaKind.Document);
        profile.Media.Count.ShouldBe(3);
    }

    [Fact]
    public void Media_rules()
    {
        var profile = Draft();
        var fileId = Guid.CreateVersion7();
        profile.AddMedia(PartnerMediaKind.WorkExample, fileId, null);

        Should.Throw<DomainException>(() => profile.AddMedia(PartnerMediaKind.Document, fileId, null)).Code.ShouldBe("partner.media_duplicate");
        Should.Throw<DomainException>(() => profile.AddMedia((PartnerMediaKind)7, Guid.CreateVersion7(), null)).Code.ShouldBe("partner.media_kind_invalid");
        Should.Throw<DomainException>(() => profile.AddMedia(PartnerMediaKind.WorkExample, Guid.CreateVersion7(), new string('c', PartnerProfile.CaptionMaxLength + 1)))
            .Code.ShouldBe("partner.caption_too_long");
    }

    [Theory]
    [InlineData(PartnerMediaKind.WorkExample, PartnerProfile.MaxWorkExamples)]
    [InlineData(PartnerMediaKind.Document, PartnerProfile.MaxDocuments)]
    public void Media_have_a_limit_per_kind(PartnerMediaKind kind, int limit)
    {
        var profile = Draft();
        for (var i = 0; i < limit; i++)
        {
            profile.AddMedia(kind, Guid.CreateVersion7(), null);
        }

        Should.Throw<DomainException>(() => profile.AddMedia(kind, Guid.CreateVersion7(), null)).Code.ShouldBe("partner.too_many_media");
    }

    [Fact]
    public void Media_are_removed_by_id()
    {
        var profile = Draft();
        var media = profile.AddMedia(PartnerMediaKind.WorkExample, Guid.CreateVersion7(), null);

        profile.RemoveMedia(media.Id);

        profile.Media.ShouldBeEmpty();
        Should.Throw<DomainException>(() => profile.RemoveMedia(media.Id)).Code.ShouldBe("partner.media_not_found");
    }

    [Fact]
    public void A_draft_lists_what_is_missing_before_it_can_be_submitted()
    {
        var profile = Draft();
        profile.UpdateDetails(PartnerType.Specialist, "Aram", new string('a', PartnerProfile.AboutMinLengthToSubmit - 1), null, null);
        profile.AddMedia(PartnerMediaKind.Document, Guid.CreateVersion7(), null);

        profile.MissingForSubmit().ShouldBe(new[] { "about", "services", "areas", "work_examples" });
        Should.Throw<DomainException>(() => profile.Submit(Now)).Code.ShouldBe("partner.incomplete");
        profile.Status.ShouldBe(PartnerStatus.Draft);
    }

    [Fact]
    public void A_complete_draft_is_submitted_for_review()
    {
        var profile = Complete();
        profile.CanSubmit.ShouldBeTrue();

        profile.Submit(Now);

        profile.Status.ShouldBe(PartnerStatus.UnderReview);
        profile.SubmittedAt.ShouldBe(Now);
        profile.CanEdit.ShouldBeFalse();
        profile.CanSubmit.ShouldBeFalse();
        var change = profile.StatusChanges.ShouldHaveSingleItem();
        change.Sequence.ShouldBe(1);
        change.FromStatus.ShouldBe(PartnerStatus.Draft);
        change.ToStatus.ShouldBe(PartnerStatus.UnderReview);
        change.Comment.ShouldBeNull();
        change.PartnerProfileId.ShouldBe(profile.Id);
    }

    [Fact]
    public void A_profile_under_review_cannot_be_changed_or_submitted_again()
    {
        var profile = UnderReview();

        Should.Throw<DomainException>(() => profile.UpdateDetails(PartnerType.Specialist, "Aram", null, null, null)).Code.ShouldBe("partner.not_editable");
        Should.Throw<DomainException>(() => profile.SetServices([Heating])).Code.ShouldBe("partner.not_editable");
        Should.Throw<DomainException>(() => profile.SetAreas([(Masis, null)])).Code.ShouldBe("partner.not_editable");
        Should.Throw<DomainException>(() => profile.AddMedia(PartnerMediaKind.WorkExample, Guid.CreateVersion7(), null)).Code.ShouldBe("partner.not_editable");
        Should.Throw<DomainException>(() => profile.RemoveMedia(profile.Media.First().Id)).Code.ShouldBe("partner.not_editable");
        Should.Throw<DomainException>(() => profile.Submit(Now)).Code.ShouldBe("partner.cannot_submit");
    }

    [Fact]
    public void Approval_makes_the_profile_public_and_it_stays_editable()
    {
        var profile = UnderReview();

        profile.Approve(Now.AddDays(1));

        profile.Status.ShouldBe(PartnerStatus.Approved);
        profile.ApprovedAt.ShouldBe(Now.AddDays(1));
        profile.CanEdit.ShouldBeTrue();
        profile.CanSubmit.ShouldBeFalse();
        profile.SetServices([Plumbing, Heating]);
        profile.Status.ShouldBe(PartnerStatus.Approved);
    }

    [Fact]
    public void The_type_is_fixed_after_approval()
    {
        var profile = Approved();

        Should.Throw<DomainException>(() => profile.UpdateDetails(PartnerType.Company, "Aram", LongAbout, null, null)).Code.ShouldBe("partner.type_locked");
        Should.NotThrow(() => profile.UpdateDetails(PartnerType.Specialist, "Aram Pro", LongAbout, null, null));
    }

    [Fact]
    public void Staff_can_ask_for_changes_and_the_partner_resubmits()
    {
        var profile = UnderReview();

        profile.RequestChanges("  Add a photo of finished work, please. ");

        profile.Status.ShouldBe(PartnerStatus.NeedsChanges);
        profile.ReviewComment.ShouldBe("Add a photo of finished work, please.");
        profile.CanEdit.ShouldBeTrue();
        profile.CanSubmit.ShouldBeTrue();

        profile.Submit(Now.AddHours(2));

        profile.Status.ShouldBe(PartnerStatus.UnderReview);
        profile.ReviewComment.ShouldBeNull();
        profile.SubmittedAt.ShouldBe(Now.AddHours(2));
        profile.StatusChanges.Select(c => c.Sequence).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Rejection_is_final_and_explained()
    {
        var profile = UnderReview();

        profile.Reject("Not a real business.");

        profile.Status.ShouldBe(PartnerStatus.Rejected);
        profile.ReviewComment.ShouldBe("Not a real business.");
        profile.CanEdit.ShouldBeFalse();
        Should.Throw<DomainException>(() => profile.Submit(Now)).Code.ShouldBe("partner.cannot_submit");
    }

    [Fact]
    public void Approved_partners_can_be_suspended_and_reinstated()
    {
        var profile = Approved();
        var approvedAt = profile.ApprovedAt;

        profile.Suspend("Expired license.");
        profile.Status.ShouldBe(PartnerStatus.Suspended);
        profile.ReviewComment.ShouldBe("Expired license.");
        profile.CanEdit.ShouldBeFalse();

        profile.Reinstate();
        profile.Status.ShouldBe(PartnerStatus.Approved);
        profile.ReviewComment.ShouldBeNull();
        profile.ApprovedAt.ShouldBe(approvedAt);
    }

    [Fact]
    public void Staff_decisions_need_the_right_status()
    {
        var draft = Draft();
        Should.Throw<DomainException>(() => draft.Approve(Now)).Code.ShouldBe("partner.not_under_review");
        Should.Throw<DomainException>(() => draft.RequestChanges("x")).Code.ShouldBe("partner.not_under_review");
        Should.Throw<DomainException>(() => draft.Reject("x")).Code.ShouldBe("partner.not_under_review");
        Should.Throw<DomainException>(() => draft.Suspend("x")).Code.ShouldBe("partner.not_approved");
        Should.Throw<DomainException>(() => draft.Reinstate()).Code.ShouldBe("partner.not_suspended");
    }

    [Fact]
    public void Asking_for_changes_rejecting_and_suspending_need_a_comment()
    {
        Should.Throw<DomainException>(() => UnderReview().RequestChanges(" ")).Code.ShouldBe("partner.comment_required");
        Should.Throw<DomainException>(() => UnderReview().Reject(null!)).Code.ShouldBe("partner.comment_required");
        Should.Throw<DomainException>(() => Approved().Suspend(new string('x', PartnerStatusChange.CommentMaxLength + 1)))
            .Code.ShouldBe("partner.comment_too_long");
    }

    [Fact]
    public void A_new_profile_gets_a_readable_unique_slug()
    {
        var profile = PartnerProfile.Create(UserId, PartnerType.Specialist, "Արամ Սանտեխնիկ");

        profile.Slug.ShouldBe("aram-santekhnik-" + profile.Id.ToString("N")[^6..]);
        Slug.IsValid(profile.Slug).ShouldBeTrue();
        PartnerProfile.Create(UserId, PartnerType.Specialist, "Արամ Սանտեխնիկ").Slug.ShouldNotBe(profile.Slug);
    }

    [Fact]
    public void Renaming_keeps_the_slug_so_links_keep_working()
    {
        var profile = Draft();
        var slug = profile.Slug;

        profile.UpdateDetails(PartnerType.Specialist, "Completely New Name", null, null, null);

        profile.Slug.ShouldBe(slug);
    }

    [Fact]
    public void A_name_with_no_Latin_equivalent_still_gets_a_slug()
    {
        var profile = PartnerProfile.Create(UserId, PartnerType.Specialist, "☺☺");

        profile.Slug.ShouldBe("partner-" + profile.Id.ToString("N")[^6..]);
    }

    [Fact]
    public void Slugs_stay_within_their_length()
    {
        PartnerProfile.Create(UserId, PartnerType.Company, new string('a', PartnerProfile.DisplayNameMaxLength)).Slug!.Length
            .ShouldBeLessThanOrEqualTo(PartnerProfile.SlugMaxLength);
    }

    [Fact]
    public void A_partner_is_paused_for_debt_and_resumed_once_each()
    {
        var profile = Approved();
        var now = new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

        profile.ResumeAfterDebt().ShouldBeFalse();
        profile.PauseForDebt(now).ShouldBeTrue();
        profile.PauseForDebt(now.AddDays(1)).ShouldBeFalse();
        profile.DebtPausedSince.ShouldBe(now);

        profile.ResumeAfterDebt().ShouldBeTrue();
        profile.DebtPausedSince.ShouldBeNull();
    }
}
