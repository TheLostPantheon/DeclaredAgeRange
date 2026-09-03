# DeclaredAgeRange

.NET binding for Apple's [Declared Age Range](https://developer.apple.com/documentation/declaredagerange/) API on iOS 26+ and macOS 26+. Request the signed-in user's age range with one `await` and deliver an age-appropriate experience without learning their exact age.

```csharp
using DeclaredAgeRange;

if (AgeRangeService.IsSupported)                       // OS 26+ on real hardware
{
    try
    {
        var response = await AgeRangeService.RequestAgeRangeAsync(ageGate: 16);

        if (response is AgeRangeResponse.Sharing { AgeRange: var range } && range.IsAtLeast(16))
            ShowFullExperience();
        else
            ShowRestrictedExperience();               // declined, or under 16
    }
    catch (AgeRangeException ex)
    {
        // ex.Error: NotAvailable, InvalidRequest, InvalidAccount, DeclinedOnboarding, Network, Unknown
    }
}
```

## Setup

1. Add `com.apple.developer.declared-age-range` (`true`) to your app's `Entitlements.plist`.
2. Enable the **Declared Age Range** capability on the app's identifier in the Apple Developer portal and regenerate the provisioning profile.

The API only works on a physical device. On the Simulator `IsSupported` is `false` and requests fail with `AgeRangeError.NotAvailable`.

Full documentation, source and sample: https://github.com/TheLostPantheon/DeclaredAgeRange
