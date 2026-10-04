namespace HomeServices.Domain.Tests;

public class DomainExceptionTests
{
    [Fact]
    public void Carries_a_stable_code_and_a_message()
    {
        var exception = new DomainException("order.not_in_progress", "The order has not started yet.");

        exception.Code.ShouldBe("order.not_in_progress");
        exception.Message.ShouldBe("The order has not started yet.");
    }
}
