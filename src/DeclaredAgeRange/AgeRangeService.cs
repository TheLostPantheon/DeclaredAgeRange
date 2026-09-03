using CoreFoundation;
using Foundation;

#if __IOS__
using PlatformAnchor = UIKit.UIViewController;
#elif __MACOS__
using PlatformAnchor = AppKit.NSWindow;
#endif

namespace DeclaredAgeRange;

/// <summary>
/// .NET counterpart of Apple's <c>AgeRangeService</c>. Requests the signed-in user's age range
/// so your app can deliver an age-appropriate experience without learning their exact age.
/// </summary>
/// <remarks>
/// Requires iOS 26 / macOS 26, a physical device, and the
/// <c>com.apple.developer.declared-age-range</c> entitlement on the app. Check <see cref="IsSupported"/> first.
/// </remarks>
public static class AgeRangeService
{
    /// <summary>
    /// True when a request can succeed on this device: iOS 26+ / macOS 26+ on real hardware.
    /// Always false on the iOS Simulator, where Apple's service reports <see cref="AgeRangeError.NotAvailable"/>.
    /// </summary>
    public static bool IsSupported =>
#if __IOS__
        OperatingSystem.IsIOSVersionAtLeast(26) && ObjCRuntime.Runtime.Arch != ObjCRuntime.Arch.SIMULATOR;
#elif __MACOS__
        OperatingSystem.IsMacOSVersionAtLeast(26);
#else
        false;
#endif

    /// <summary>
    /// Requests the user's age range, presenting Apple's system UI from the app's current window.
    /// </summary>
    /// <param name="ageGate">The required minimum age for your app (for example 13, 16 or 18).</param>
    /// <param name="secondAgeGate">An optional additional age threshold.</param>
    /// <param name="thirdAgeGate">An optional additional age threshold.</param>
    /// <param name="cancellationToken">Stops waiting for the result. Apple's UI cannot be dismissed programmatically.</param>
    /// <exception cref="AgeRangeException">The request failed; see <see cref="AgeRangeException.Error"/>.</exception>
    public static Task<AgeRangeResponse> RequestAgeRangeAsync(
        int ageGate,
        int? secondAgeGate = null,
        int? thirdAgeGate = null,
        CancellationToken cancellationToken = default)
        => RequestAgeRangeAsync(ageGate, secondAgeGate, thirdAgeGate, anchor: null, cancellationToken);

    /// <summary>
    /// Requests the user's age range, presenting Apple's system UI from <paramref name="anchor"/>
    /// (a <c>UIViewController</c> on iOS, an <c>NSWindow</c> on macOS). Pass <see langword="null"/> to resolve it automatically.
    /// </summary>
    /// <inheritdoc cref="RequestAgeRangeAsync(int, int?, int?, CancellationToken)"/>
    public static async Task<AgeRangeResponse> RequestAgeRangeAsync(
        int ageGate,
        int? secondAgeGate,
        int? thirdAgeGate,
        PlatformAnchor? anchor,
        CancellationToken cancellationToken = default)
    {
        if (ageGate <= 0)
            throw new ArgumentOutOfRangeException(nameof(ageGate), ageGate, "Age gate must be a positive age.");
        if (!IsSupported)
            throw new AgeRangeException(AgeRangeError.NotAvailable,
                "Declared Age Range is not available here. It requires a physical device running iOS 26 or macOS 26; " +
                "the Simulator is not supported. Check AgeRangeService.IsSupported before calling.");

        cancellationToken.ThrowIfCancellationRequested();

        var tcs = new TaskCompletionSource<AgeRangeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        RunOnMainThread(() =>
        {
            try
            {
                var resolvedAnchor = anchor ?? PlatformAnchorResolver.Resolve()
                    ?? throw new AgeRangeException(AgeRangeError.InvalidRequest,
                        "No window is available to present the age range UI from. Pass an anchor explicitly.");

                DARAgeRangeService.RequestAgeRange(
                    ageGate,
                    ToNSNumber(secondAgeGate),
                    ToNSNumber(thirdAgeGate),
                    resolvedAnchor,
                    (response, error) =>
                    {
                        if (error is not null || response is null)
                            tcs.TrySetException(AgeRangeException.FromNSError(error));
                        else
                            tcs.TrySetResult(ToManaged(response));
                    });
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    static void RunOnMainThread(Action action)
    {
        if (NSThread.IsMain)
            action();
        else
            DispatchQueue.MainQueue.DispatchAsync(action);
    }

    static NSNumber? ToNSNumber(int? value) => value is { } v ? NSNumber.FromInt32(v) : null;

    static AgeRangeResponse ToManaged(DARAgeRangeResponse response)
    {
        if (response.Type == DARAgeRangeResponseType.Sharing && response.AgeRange is { } native)
        {
            var range = new AgeRange(
                native.LowerBound?.Int32Value,
                native.UpperBound?.Int32Value,
                NativeMapping.ToDeclaration(native.AgeRangeDeclaration));
            return new AgeRangeResponse.Sharing(range);
        }

        return AgeRangeResponse.DeclinedSharing.Instance;
    }
}
