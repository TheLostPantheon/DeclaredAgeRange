namespace DeclaredAgeRange;

// The exception type itself is pure managed code so it can be unit-tested on any platform.
// The NSError factory lives in AgeRangeException.Apple.cs, which needs Foundation.

/// <summary>Thrown when an age range request fails.</summary>
public sealed partial class AgeRangeException : Exception
{
    /// <summary>The classified failure reason.</summary>
    public AgeRangeError Error { get; }

    /// <summary>Creates an exception with a classified <paramref name="error"/>, a message, and an optional underlying cause.</summary>
    public AgeRangeException(AgeRangeError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }
}
