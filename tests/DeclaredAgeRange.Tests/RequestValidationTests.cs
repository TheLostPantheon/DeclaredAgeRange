using DeclaredAgeRange;
using Xunit;

namespace DeclaredAgeRange.Tests;

// RequestValidation is the half of AgeRangeService that needs no Apple frameworks. Everything here
// runs on plain .NET; the native request path is exercised by the samples on a device instead.
public class RequestValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Age_gate_must_be_positive(int ageGate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => RequestValidation.Validate(ageGate, isSupported: true));

        Assert.Equal("ageGate", ex.ParamName);
        Assert.Equal(ageGate, ex.ActualValue);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(16)]
    [InlineData(18)]
    public void A_positive_age_gate_on_a_supported_platform_is_allowed(int ageGate)
    {
        RequestValidation.Validate(ageGate, isSupported: true);
    }

    [Fact]
    public void An_unsupported_platform_is_reported_as_NotAvailable()
    {
        var ex = Assert.Throws<AgeRangeException>(() => RequestValidation.Validate(13, isSupported: false));

        Assert.Equal(AgeRangeError.NotAvailable, ex.Error);
        Assert.Equal(RequestValidation.NotAvailableMessage, ex.Message);
    }

    [Fact]
    public void The_message_points_the_caller_at_IsSupported()
    {
        // The whole value of this error is telling someone what to check instead, so assert it says so.
        Assert.Contains("IsSupported", RequestValidation.NotAvailableMessage);
    }

    [Fact]
    public void A_bad_age_gate_is_reported_before_platform_support()
    {
        // Otherwise a caller on the Simulator would never learn their argument was wrong.
        Assert.Throws<ArgumentOutOfRangeException>(() => RequestValidation.Validate(0, isSupported: false));
    }
}

public class AgeRangeExceptionTests
{
    [Fact]
    public void Carries_the_classified_error_and_message()
    {
        var ex = new AgeRangeException(AgeRangeError.InvalidAccount, "nope");

        Assert.Equal(AgeRangeError.InvalidAccount, ex.Error);
        Assert.Equal("nope", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Keeps_the_underlying_cause()
    {
        var cause = new InvalidOperationException("underlying");

        var ex = new AgeRangeException(AgeRangeError.Network, "wrapped", cause);

        Assert.Same(cause, ex.InnerException);
    }
}
