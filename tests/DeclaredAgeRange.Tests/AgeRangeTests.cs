using Xunit;

namespace DeclaredAgeRange.Tests;

public class AgeRangeTests
{
    // Apple returns bounds aligned to the age gates you pass. With a single gate of 16:
    //   at or above the gate -> lowerBound = 16, upperBound = nil
    //   below the gate       -> lowerBound = nil, upperBound = 15
    static readonly AgeRange Adult = new(16, null, AgeRangeDeclaration.SelfDeclared);
    static readonly AgeRange Minor = new(null, 15, AgeRangeDeclaration.GuardianDeclared);
    static readonly AgeRange Teen = new(13, 15, AgeRangeDeclaration.GuardianDeclared);   // gates 13 and 16

    [Fact]
    public void IsAtLeast_uses_lower_bound()
    {
        Assert.True(Adult.IsAtLeast(16));
        Assert.True(Adult.IsAtLeast(13));
        Assert.False(Adult.IsAtLeast(18));       // 16+ does not prove 18+

        Assert.False(Minor.IsAtLeast(16));
        Assert.False(Minor.IsAtLeast(1));        // unknown lower bound proves nothing

        Assert.True(Teen.IsAtLeast(13));
        Assert.False(Teen.IsAtLeast(16));
    }

    [Fact]
    public void IsUnder_uses_upper_bound()
    {
        Assert.True(Minor.IsUnder(16));
        Assert.True(Minor.IsUnder(18));
        Assert.False(Minor.IsUnder(15));         // upper bound 15 means could be exactly 15

        Assert.False(Adult.IsUnder(18));         // no upper bound proves nothing
        Assert.False(Adult.IsUnder(100));

        Assert.True(Teen.IsUnder(16));
        Assert.False(Teen.IsUnder(13));
    }

    [Fact]
    public void ToString_formats_bounds_and_declaration()
    {
        Assert.Equal("16–∞ (SelfDeclared)", Adult.ToString());
        Assert.Equal("?–15 (GuardianDeclared)", Minor.ToString());
        Assert.Equal("13–15 (undeclared)", new AgeRange(13, 15, null).ToString());
    }

    [Fact]
    public void Records_compare_by_value()
    {
        Assert.Equal(new AgeRange(16, null, AgeRangeDeclaration.SelfDeclared), Adult);
        Assert.NotEqual(Adult, Minor);
    }
}

public class AgeRangeResponseTests
{
    [Fact]
    public void Sharing_carries_the_range()
    {
        var range = new AgeRange(16, null, AgeRangeDeclaration.Confirmed);
        AgeRangeResponse response = new AgeRangeResponse.Sharing(range);

        Assert.True(response.IsSharing);
        var sharing = Assert.IsType<AgeRangeResponse.Sharing>(response);
        Assert.Same(range, sharing.AgeRange);
    }

    [Fact]
    public void DeclinedSharing_is_a_singleton_without_a_range()
    {
        AgeRangeResponse response = AgeRangeResponse.DeclinedSharing.Instance;

        Assert.False(response.IsSharing);
        Assert.Same(AgeRangeResponse.DeclinedSharing.Instance, response);
        Assert.Equal(AgeRangeResponse.DeclinedSharing.Instance, new AgeRangeResponse.DeclinedSharing());
    }

    [Fact]
    public void Pattern_matching_reads_naturally()
    {
        AgeRangeResponse response = new AgeRangeResponse.Sharing(new AgeRange(16, null, null));

        var allowed = response is AgeRangeResponse.Sharing { AgeRange: var r } && r.IsAtLeast(16);

        Assert.True(allowed);
    }
}

public class NativeMappingTests
{
    [Theory]
    [InlineData(0, null)]                                  // None
    [InlineData(1, AgeRangeDeclaration.SelfDeclared)]
    [InlineData(2, AgeRangeDeclaration.GuardianDeclared)]
    [InlineData(3, AgeRangeDeclaration.Confirmed)]
    [InlineData(4, AgeRangeDeclaration.Unknown)]
    [InlineData(99, AgeRangeDeclaration.Unknown)]          // a value added by a future bridge
    public void Declaration_maps_every_native_value(long native, AgeRangeDeclaration? expected)
    {
        Assert.Equal(expected, NativeMapping.ToDeclaration((DARAgeRangeDeclaration)native));
    }

    [Theory]
    [InlineData(0, AgeRangeError.Unknown)]
    [InlineData(1, AgeRangeError.NotAvailable)]
    [InlineData(2, AgeRangeError.InvalidRequest)]
    [InlineData(3, AgeRangeError.InvalidAccount)]
    [InlineData(4, AgeRangeError.DeclinedOnboarding)]
    [InlineData(5, AgeRangeError.Network)]
    public void Error_codes_in_the_bridge_domain_map_one_to_one(long code, AgeRangeError expected)
    {
        Assert.Equal(expected, NativeMapping.ToError(NativeMapping.ErrorDomain, code));
    }

    [Theory]
    [InlineData(NativeMapping.ErrorDomain, 42)]        // code the package does not know
    [InlineData(NativeMapping.ErrorDomain, -1)]
    [InlineData("NSCocoaErrorDomain", 1)]              // right code, wrong domain
    [InlineData(null, 1)]
    public void Anything_else_is_unknown(string? domain, long code)
    {
        Assert.Equal(AgeRangeError.Unknown, NativeMapping.ToError(domain, code));
    }

    [Fact]
    public void Native_enum_values_match_the_swift_bridge()
    {
        // These numbers are the contract with DeclaredAgeRangeWrapper.swift. Change both or neither.
        Assert.Equal(0, (long)DARAgeRangeDeclaration.None);
        Assert.Equal(1, (long)DARAgeRangeDeclaration.SelfDeclared);
        Assert.Equal(2, (long)DARAgeRangeDeclaration.GuardianDeclared);
        Assert.Equal(3, (long)DARAgeRangeDeclaration.Confirmed);
        Assert.Equal(4, (long)DARAgeRangeDeclaration.Unknown);
        Assert.Equal(0, (long)DARAgeRangeResponseType.Sharing);
        Assert.Equal(1, (long)DARAgeRangeResponseType.DeclinedSharing);
    }
}
