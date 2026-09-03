namespace DeclaredAgeRange;

// Pure managed model types. No Apple dependencies, so they are unit-testable on any platform.

/// <summary>
/// The age range Apple shared for the signed-in user. Mirrors <c>AgeRangeService.AgeRange</c>.
/// Bounds correspond to the age gates you passed, never the person's exact age.
/// </summary>
/// <param name="LowerBound">Minimum age in the range, or <see langword="null"/> if unbounded.</param>
/// <param name="UpperBound">Maximum age in the range, or <see langword="null"/> if unbounded (adult).</param>
/// <param name="AgeRangeDeclaration">How the range was declared, or <see langword="null"/> if Apple did not say.</param>
public sealed record AgeRange(int? LowerBound, int? UpperBound, AgeRangeDeclaration? AgeRangeDeclaration)
{
    /// <summary>True when the user is known to be at least <paramref name="age"/> years old.</summary>
    public bool IsAtLeast(int age) => LowerBound is { } lower && lower >= age;

    /// <summary>True when the user is known to be under <paramref name="age"/> years old.</summary>
    public bool IsUnder(int age) => UpperBound is { } upper && upper < age;

    /// <summary>Formats the range as <c>lower–upper (declaration)</c>, using ∞ for no upper bound.</summary>
    public override string ToString() =>
        $"{LowerBound?.ToString() ?? "?"}–{UpperBound?.ToString() ?? "∞"} ({AgeRangeDeclaration?.ToString() ?? "undeclared"})";
}

/// <summary>Mirrors <c>AgeRangeService.AgeRangeDeclaration</c>.</summary>
public enum AgeRangeDeclaration
{
    /// <summary>The person set their own age range when signing in to iCloud.</summary>
    SelfDeclared,
    /// <summary>A parent, guardian, or Family Organizer set the age range.</summary>
    GuardianDeclared,
    /// <summary>The age range was confirmed by a scrutinized method such as a payment card or government ID.</summary>
    Confirmed,
    /// <summary>A declaration type this package does not know about yet.</summary>
    Unknown,
}

/// <summary>
/// Result of an age range request. Mirrors <c>AgeRangeService.Response</c>:
/// either <see cref="Sharing"/> with an <see cref="AgeRange"/>, or <see cref="DeclinedSharing"/>.
/// </summary>
public abstract record AgeRangeResponse
{
    private AgeRangeResponse() { }

    /// <summary>True when the user agreed to share and <see cref="Sharing.AgeRange"/> is available.</summary>
    public bool IsSharing => this is Sharing;

    /// <summary>The user agreed to share their age range.</summary>
    public sealed record Sharing(AgeRange AgeRange) : AgeRangeResponse;

    /// <summary>The user declined to share their age range.</summary>
    public sealed record DeclinedSharing : AgeRangeResponse
    {
        /// <summary>The shared singleton instance.</summary>
        public static DeclinedSharing Instance { get; } = new();
    }
}

/// <summary>Mirrors <c>AgeRangeService.Error</c>. Values match the native bridge's error codes.</summary>
public enum AgeRangeError
{
    /// <summary>An error this package could not classify. See the inner exception.</summary>
    Unknown = 0,
    /// <summary>The system could not share the age range, or the OS is older than 26.</summary>
    NotAvailable = 1,
    /// <summary>The request had invalid parameters, or the app lacks the entitlement.</summary>
    InvalidRequest = 2,
    /// <summary>The current Apple Account is not eligible for age range sharing.</summary>
    InvalidAccount = 3,
    /// <summary>The person declined the age range onboarding flow.</summary>
    DeclinedOnboarding = 4,
    /// <summary>A network or server issue prevented completing the request.</summary>
    Network = 5,
}
