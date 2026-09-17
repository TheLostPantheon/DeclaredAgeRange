using Foundation;

namespace DeclaredAgeRange;

// The half of AgeRangeException that needs Apple's frameworks. Kept apart from the type itself
// so the rest stays testable on plain .NET.
public sealed partial class AgeRangeException
{
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
