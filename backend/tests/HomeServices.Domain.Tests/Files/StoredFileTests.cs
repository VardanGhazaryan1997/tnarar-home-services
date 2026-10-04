using HomeServices.Domain.Files;

namespace HomeServices.Domain.Tests.Files;

public class StoredFileTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.FromHours(4));

    private static StoredFile Photo(long size = 1000) =>
        StoredFile.Begin(FileOwnerType.User, "user-1", "kitchen.jpg", "image/jpeg", size, Now);

    [Fact]
    public void A_new_upload_is_pending_with_a_key_under_its_own_folder()
    {
        var file = StoredFile.Begin(FileOwnerType.Staff, "staff-1", "Contract.PDF", " Application/PDF ", 2048, Now);

        file.OwnerType.ShouldBe(FileOwnerType.Staff);
        file.OwnerId.ShouldBe("staff-1");
        file.Kind.ShouldBe(FileKind.Document);
        file.Status.ShouldBe(FileStatus.Pending);
        file.ContentType.ShouldBe("application/pdf");
        file.Size.ShouldBe(2048);
        file.OriginalFileName.ShouldBe("Contract.PDF");
        file.Key.ShouldBe($"files/2026/10/{file.Id:N}/original.pdf");
        file.ThumbnailKeyFor().ShouldBe($"files/2026/10/{file.Id:N}/thumbnail.jpg");
        file.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void The_key_uses_the_UTC_month()
    {
        var lateEvening = new DateTimeOffset(2026, 11, 1, 2, 0, 0, TimeSpan.FromHours(4)); // 31 October in UTC

        StoredFile.Begin(FileOwnerType.User, "u", "a.png", "image/png", 1, lateEvening).Key.ShouldStartWith("files/2026/10/");
    }

    [Theory]
    [InlineData("image/jpeg", FileKind.Image, ".jpg")]
    [InlineData("image/png", FileKind.Image, ".png")]
    [InlineData("image/webp", FileKind.Image, ".webp")]
    [InlineData("video/mp4", FileKind.Video, ".mp4")]
    [InlineData("video/quicktime", FileKind.Video, ".mov")]
    [InlineData("application/pdf", FileKind.Document, ".pdf")]
    public void Allowed_types_get_their_kind_and_extension(string contentType, FileKind kind, string extension)
    {
        var file = StoredFile.Begin(FileOwnerType.User, "u", "x", contentType, 1, Now);

        file.Kind.ShouldBe(kind);
        file.Key.ShouldEndWith("/original" + extension);
        StoredFile.KindOf(contentType.ToUpperInvariant()).ShouldBe(kind);
        StoredFile.IsAllowedType(contentType).ShouldBeTrue();
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/x-msdownload")]
    [InlineData("")]
    public void Other_types_are_refused(string contentType)
    {
        StoredFile.IsAllowedType(contentType).ShouldBeFalse();
        Should.Throw<DomainException>(() => StoredFile.Begin(FileOwnerType.User, "u", "x", contentType, 1, Now))
            .Code.ShouldBe("file.type_not_allowed");
        Should.Throw<DomainException>(() => StoredFile.KindOf(contentType)).Code.ShouldBe("file.type_not_allowed");
    }

    [Fact]
    public void Null_is_not_an_allowed_type()
    {
        StoredFile.IsAllowedType(null).ShouldBeFalse();
    }

    [Fact]
    public void Each_kind_has_a_size_limit()
    {
        StoredFile.MaxSizeFor(FileKind.Image).ShouldBe(10L * 1024 * 1024);
        StoredFile.MaxSizeFor(FileKind.Video).ShouldBe(200L * 1024 * 1024);
        StoredFile.MaxSizeFor(FileKind.Document).ShouldBe(10L * 1024 * 1024);

        Should.NotThrow(() => Photo(StoredFile.MaxSizeFor(FileKind.Image)));
        Should.Throw<DomainException>(() => Photo(StoredFile.MaxSizeFor(FileKind.Image) + 1)).Code.ShouldBe("file.too_large");
        Should.Throw<DomainException>(() => Photo(0)).Code.ShouldBe("file.too_large");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_file_needs_an_owner(string ownerId)
    {
        Should.Throw<DomainException>(() => StoredFile.Begin(FileOwnerType.User, ownerId, "a.jpg", "image/jpeg", 1, Now))
            .Code.ShouldBe("file.owner_required");
        Should.Throw<DomainException>(() => StoredFile.Begin(FileOwnerType.User, new string('x', StoredFile.OwnerIdMaxLength + 1), "a.jpg", "image/jpeg", 1, Now))
            .Code.ShouldBe("file.owner_required");
    }

    [Theory]
    [InlineData("C:\\Users\\ani\\Desktop\\photo.jpg", "photo.jpg")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("  tab\tbed.jpg ", "tabbed.jpg")]
    [InlineData("", "file")]
    [InlineData("folder/", "file")]
    public void File_names_keep_only_the_name_part(string given, string expected)
    {
        StoredFile.Begin(FileOwnerType.User, "u", given, "image/jpeg", 1, Now).OriginalFileName.ShouldBe(expected);
    }

    [Fact]
    public void Long_file_names_keep_their_end_so_the_extension_survives()
    {
        var name = new string('a', 300) + ".jpg";

        var file = StoredFile.Begin(FileOwnerType.User, "u", name, "image/jpeg", 1, Now);

        file.OriginalFileName.Length.ShouldBe(StoredFile.FileNameMaxLength);
        file.OriginalFileName.ShouldEndWith(".jpg");
    }

    [Fact]
    public void Only_the_owner_owns_a_file()
    {
        var file = Photo();

        file.IsOwnedBy(FileOwnerType.User, "user-1").ShouldBeTrue();
        file.IsOwnedBy(FileOwnerType.Staff, "user-1").ShouldBeFalse();
        file.IsOwnedBy(FileOwnerType.User, "user-2").ShouldBeFalse();
        file.IsOwnedBy(FileOwnerType.User, null).ShouldBeFalse();
    }

    [Fact]
    public void A_ready_image_records_its_real_size_dimensions_and_thumbnail()
    {
        var file = Photo(size: 1000);
        var later = Now.AddMinutes(2);

        file.MarkReady(980, later, file.ThumbnailKeyFor(), 4000, 3000);

        file.Status.ShouldBe(FileStatus.Ready);
        file.Size.ShouldBe(980);
        file.ThumbnailKey.ShouldBe(file.ThumbnailKeyFor());
        file.Width.ShouldBe(4000);
        file.Height.ShouldBe(3000);
        file.CompletedAt.ShouldBe(later);
    }

    [Fact]
    public void A_ready_document_has_no_thumbnail()
    {
        var file = StoredFile.Begin(FileOwnerType.User, "u", "a.pdf", "application/pdf", 10, Now);

        file.MarkReady(10, Now);

        file.ThumbnailKey.ShouldBeNull();
        file.Width.ShouldBeNull();
    }

    [Fact]
    public void A_rejected_upload_records_why()
    {
        var file = Photo();

        file.Reject("file.invalid_content", Now);

        file.Status.ShouldBe(FileStatus.Rejected);
        file.RejectionCode.ShouldBe("file.invalid_content");
        file.CompletedAt.ShouldBe(Now);
    }

    [Fact]
    public void An_upload_is_completed_only_once()
    {
        var ready = Photo();
        ready.MarkReady(1, Now);
        var rejected = Photo();
        rejected.Reject("file.too_large", Now);

        Should.Throw<DomainException>(() => ready.MarkReady(1, Now)).Code.ShouldBe("file.already_completed");
        Should.Throw<DomainException>(() => ready.Reject("x", Now)).Code.ShouldBe("file.already_completed");
        Should.Throw<DomainException>(() => rejected.MarkReady(1, Now)).Code.ShouldBe("file.already_completed");
    }
}
