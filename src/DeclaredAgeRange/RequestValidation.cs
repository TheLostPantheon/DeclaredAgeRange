namespace DeclaredAgeRange;

/// <summary>
/// Argument and availability guards for an age range request. Pure managed logic with no Apple
/// dependencies, so every rejection path is unit-tested without a device.
/// </summary>
internal static class RequestValidation
{
    /// <summary>Message used when the platform cannot serve a request. Shared with the tests so the wording stays asserted.</summary>
    internal const string NotAvailableMessage =
        "Declared Age Range is not available here. It requires a physical device running iOS 26 or macOS 26; " +
        "the Simulator is not supported. Check AgeRangeService.IsSupported before calling.";

    /// <summary>
    /// Throws if the request cannot proceed. The age gate is checked before platform support, so a caller
    /// passing a bad argument hears about the argument even on a device that could never serve the request.
    /// </summary>
    /// <param name="ageGate">The required minimum age. Must be positive.</param>
    /// <param name="isSupported">Whether this OS and device can serve a request.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ageGate"/> is not a positive age.</exception>
    /// <exception cref="AgeRangeException">The platform cannot serve the request.</exception>
    public static void Validate(int ageGate, bool isSupported)
    {
        if (ageGate <= 0)
            throw new ArgumentOutOfRangeException(nameof(ageGate), ageGate, "Age gate must be a positive age.");

        if (!isSupported)
            throw new AgeRangeException(AgeRangeError.NotAvailable, NotAvailableMessage);
    }
}
