using HomeServices.Application.Files;
using HomeServices.Application.Tests.Support;

namespace HomeServices.Application.Tests.Files;

public class FileSignaturesTests
{
    public static TheoryData<string, byte[]> RealFiles => new()
    {
        { "image/jpeg", SampleFiles.Jpeg },
        { "image/png", SampleFiles.Png },
        { "image/webp", SampleFiles.Webp },
        { "video/mp4", SampleFiles.Mp4 },
        { "video/quicktime", SampleFiles.QuickTime },
        { "video/quicktime", SampleFiles.OldQuickTime },
        { "application/pdf", SampleFiles.Pdf },
    };

    [Theory]
    [MemberData(nameof(RealFiles))]
    public void Real_files_match_their_type(string contentType, byte[] header)
    {
        FileSignatures.Matches(contentType, header).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RealFiles))]
    public void A_program_renamed_to_any_type_does_not_match(string contentType, byte[] header)
    {
        header.ShouldNotBeEmpty();
        FileSignatures.Matches(contentType, SampleFiles.Executable).ShouldBeFalse();
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("video/mp4")]
    [InlineData("video/quicktime")]
    [InlineData("application/pdf")]
    public void Files_too_short_to_tell_do_not_match(string contentType)
    {
        FileSignatures.Matches(contentType, [0xFF]).ShouldBeFalse();
        FileSignatures.Matches(contentType, []).ShouldBeFalse();
    }

    [Fact]
    public void A_PNG_does_not_pass_as_a_JPEG_and_unknown_types_never_match()
    {
        FileSignatures.Matches("image/jpeg", SampleFiles.Png).ShouldBeFalse();
        FileSignatures.Matches("image/gif", "GIF89a......"u8).ShouldBeFalse();
    }
}
