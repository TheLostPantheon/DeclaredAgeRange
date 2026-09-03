namespace DeclaredAgeRange;

/// <summary>Pure translation of native bridge values to public types. Unit-tested without Apple frameworks.</summary>
internal static class NativeMapping
{
    /// <summary>NSError domain used by the native bridge. Codes map 1:1 to <see cref="AgeRangeError"/>.</summary>
    public const string ErrorDomain = "DARAgeRangeErrorDomain";

    public static AgeRangeDeclaration? ToDeclaration(DARAgeRangeDeclaration declaration) => declaration switch
    {
        DARAgeRangeDeclaration.None => null,
        DARAgeRangeDeclaration.SelfDeclared => AgeRangeDeclaration.SelfDeclared,
        DARAgeRangeDeclaration.GuardianDeclared => AgeRangeDeclaration.GuardianDeclared,
        DARAgeRangeDeclaration.Confirmed => AgeRangeDeclaration.Confirmed,
        _ => AgeRangeDeclaration.Unknown,
    };

    /// <summary>Classifies an NSError by domain and code. Anything outside the bridge's domain or its known codes is <see cref="AgeRangeError.Unknown"/>.</summary>
    public static AgeRangeError ToError(string? domain, long code) =>
        domain == ErrorDomain && Enum.IsDefined(typeof(AgeRangeError), (int)code)
            ? (AgeRangeError)(int)code
            : AgeRangeError.Unknown;
}
