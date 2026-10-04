using HomeServices.Application.Files;

namespace HomeServices.Application.Tests.Files;

public class RequestUploadValidatorTests
{
    private static readonly RequestUploadValidator Validator = new();

    private static IEnumerable<string> Errors(RequestUpload command) =>
        Validator.Validate(command).Errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}");

    [Theory]
    [InlineData("photo.jpg", "image/jpeg", 10L * 1024 * 1024)]
    [InlineData("clip.mov", "video/quicktime", 200L * 1024 * 1024)]
    [InlineData("contract.pdf", "APPLICATION/PDF", 1L)]
    public void Accepts_allowed_files_within_their_limit(string fileName, string contentType, long size)
    {
        Errors(new RequestUpload(fileName, contentType, size)).ShouldBeEmpty();
    }

    [Fact]
    public void Needs_a_file_name()
    {
        Errors(new RequestUpload(" ", "image/jpeg", 1)).ShouldBe(new[] { "FileName:file_name.required" });
    }

    [Fact]
    public void Refuses_other_types()
    {
        Errors(new RequestUpload("a.gif", "image/gif", 1)).ShouldBe(new[] { "ContentType:content_type.not_allowed" });
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Needs_a_positive_size(long size)
    {
        Errors(new RequestUpload("a.jpg", "image/jpeg", size)).ShouldBe(new[] { "Size:size.invalid" });
    }

    [Fact]
    public void Each_kind_has_its_own_size_limit()
    {
        Errors(new RequestUpload("a.jpg", "image/jpeg", (10L * 1024 * 1024) + 1)).ShouldBe(new[] { "Size:size.too_large" });
        Errors(new RequestUpload("a.mp4", "video/mp4", (200L * 1024 * 1024) + 1)).ShouldBe(new[] { "Size:size.too_large" });
    }

    [Fact]
    public void The_size_limit_is_not_checked_for_a_refused_type()
    {
        Errors(new RequestUpload("a.exe", "application/x-msdownload", long.MaxValue)).ShouldBe(new[] { "ContentType:content_type.not_allowed" });
    }
}
