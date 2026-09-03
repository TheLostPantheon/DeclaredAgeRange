using Foundation;

namespace DeclaredAgeRange;

/// <summary>Thrown when an age range request fails.</summary>
public sealed class AgeRangeException : Exception
{
    /// <summary>The classified failure reason.</summary>
    public AgeRangeError Error { get; }

    /// <summary>Creates an exception with a classified <paramref name="error"/>, a message, and an optional underlying cause.</summary>
    public AgeRangeException(AgeRangeError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }

    internal static AgeRangeException FromNSError(NSError? error)
    {
        if (error is null)
            return new AgeRangeException(AgeRangeError.Unknown, "The age range request completed without a response or an error.");

        return new AgeRangeException(
            NativeMapping.ToError(error.Domain, (long)error.Code),
            error.LocalizedDescription,
            new NSErrorException(error));
    }
}
