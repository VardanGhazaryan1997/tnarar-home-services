using HomeServices.Application.Errors;

namespace HomeServices.Application.Tests.Errors;

public class AppExceptionTests
{
    [Fact]
    public void NotFound_has_a_default_code_and_accepts_a_specific_one()
    {
        new NotFoundException("Missing.").Code.ShouldBe("not_found");
        new NotFoundException("Partner not found.", "partner.not_found").Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public void Conflict_has_a_default_code_and_accepts_a_specific_one()
    {
        new ConflictException("Changed.").Code.ShouldBe("conflict");
        new ConflictException("Phone taken.", "phone.taken").Code.ShouldBe("phone.taken");
    }

    [Fact]
    public void Forbidden_has_a_default_code_and_accepts_a_specific_one()
    {
        new ForbiddenException("No.").Code.ShouldBe("forbidden");
        new ForbiddenException("Not your order.", "order.not_owner").Code.ShouldBe("order.not_owner");
    }

    [Fact]
    public void Keeps_the_message()
    {
        new NotFoundException("Partner not found.").Message.ShouldBe("Partner not found.");
    }
}
