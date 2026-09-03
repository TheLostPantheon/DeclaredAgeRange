# DeclaredAgeRange for .NET

A lightweight .NET binding for Apple's [Declared Age Range](https://developer.apple.com/documentation/declaredagerange/) API on iOS and macOS. Any .NET iOS, macOS or MAUI app can request the signed-in user's age range with one `await` and deliver an age-appropriate experience without learning their exact age.

The .NET API mirrors Apple's Swift API name-for-name: `AgeRangeService`, `AgeRangeResponse` (`Sharing` / `DeclinedSharing`), `AgeRange`, `AgeRangeDeclaration`, and `AgeRangeError`.

## Using it in your app

### 1. Reference the package

```xml
<PackageReference Include="DeclaredAgeRange" Version="1.0.0" />
```

The Swift bridge framework ships inside the package. Nothing native to build on your side.

### 2. Call it

```csharp
using DeclaredAgeRange;

if (AgeRangeService.IsSupported)                       // iOS 26+ / macOS 26+ on real hardware
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
else
{
    // Older OS or the Simulator: fall back to your existing age flow.
}
```

You do not need to pass a view controller or window. The package presents Apple's sheet from the app's key window. An overload accepts an explicit `UIViewController` (iOS) or `NSWindow` (macOS) for apps with unusual window setups.

Up to three age gates can be passed (`ageGate`, `secondAgeGate`, `thirdAgeGate`). The returned `AgeRange` bounds line up with those gates.

### 3. Add the entitlement

In your app's `Entitlements.plist`:

```xml
<key>com.apple.developer.declared-age-range</key>
<true/>
```

### 4. Enable the capability

In the Apple Developer portal, enable **Declared Age Range** on the app's identifier and regenerate the provisioning profile. Without this the request fails with `AgeRangeError.InvalidRequest` or `NotAvailable` on device.

> [!NOTE]
> The API only works on a **physical device** running iOS 26 or macOS 26. On the Simulator every request fails with
> `AgeRangeError.NotAvailable`, so `AgeRangeService.IsSupported` returns `false` there and your fallback path runs instead.

## API

| .NET | Apple | Notes |
|---|---|---|
| `AgeRangeService.IsSupported` | — | `true` on OS 26+ real hardware; `false` on the Simulator. |
| `AgeRangeService.RequestAgeRangeAsync(ageGate, secondAgeGate?, thirdAgeGate?, [anchor], [ct])` | `AgeRangeService.shared.requestAgeRange(ageGates:in:)` | Runs on the main thread for you. |
| `AgeRangeResponse.Sharing(AgeRange)` / `AgeRangeResponse.DeclinedSharing` | `AgeRangeService.Response` | `response.IsSharing` shortcut. |
| `AgeRange.LowerBound` / `UpperBound` (`int?`) | `AgeRange.lowerBound` / `upperBound` | `null` upper bound means no upper limit. |
| `AgeRange.AgeRangeDeclaration` (`AgeRangeDeclaration?`) | `AgeRange.ageRangeDeclaration` | `SelfDeclared`, `GuardianDeclared`, `Confirmed`, `Unknown`. |
| `AgeRange.IsAtLeast(age)` / `IsUnder(age)` | — | Helpers for gating. |
| `AgeRangeException.Error` (`AgeRangeError`) | `AgeRangeService.Error` | Apple's cases plus `Unknown`. |

`activeParentalControls` and the regulatory-feature APIs are not bridged in this version.

## Repository layout

```
src/DeclaredAgeRange/            The .NET package (net10.0/net9.0 × ios/macos)
│   ApiDefinition.cs             Internal Objective-C bindings
│   StructsAndEnums.cs           Internal native enums
│   AgeRangeService.cs           Public async API
│   AgeRange.cs                  Public models, enums, exception
│   PlatformAnchorResolver.cs    Finds the window/view controller to present from
native/                          Swift → Objective-C bridge, built into an xcframework
│   DeclaredAgeRangeWrapper/DeclaredAgeRangeWrapper.swift
│   DeclaredAgeRangeWrapper.xcodeproj
│   Makefile                     `make` builds iOS, iOS Simulator and macOS slices
samples/iOSSampleApp/            Minimal consumer
.github/workflows/build.yml      Builds the xcframework, packs the NuGet, publishes on tag
```

## Building

Requirements: macOS, **Xcode 26+** (full Xcode, not just Command Line Tools), **.NET 10 SDK** with the `ios` and `macos` workloads.

```bash
sudo xcode-select -s /Applications/Xcode.app
xcodebuild -downloadPlatform iOS      # once per Xcode install; the iOS platform is not bundled
dotnet workload install ios macos

# 1. Build the Swift bridge (iOS device, iOS Simulator, macOS slices + dSYMs)
make -C native

# 2. Build and pack the .NET package
dotnet pack src/DeclaredAgeRange -c Release -o artifacts

# 3. Run the sample on the Simulator (ad-hoc signed, no certificate needed; the API reports NotAvailable there)
dotnet build samples/iOSSampleApp -t:Run

# ...or on a device with the entitlement provisioned
dotnet build samples/iOSSampleApp -t:Run -r ios-arm64
```

## Using the package locally (before it is on nuget.org)

Pack it into a folder and point your app at that folder as a NuGet source:

```bash
# in this repo
dotnet pack src/DeclaredAgeRange -c Release -o ~/nuget-local -p:Version=1.0.0-local.1

# in the consuming app (once)
dotnet nuget add source ~/nuget-local --name local
dotnet add package DeclaredAgeRange --prerelease
```

Bump the `-local.N` suffix each time you repack. NuGet caches packages by version, so repacking the same version silently keeps the old bits unless you also run `dotnet nuget locals all --clear`.

## Releasing

CI builds and packs on every push. Pushing a tag `vX.Y.Z` builds the package with that version and publishes it to nuget.org, using the `NUGET_API_KEY` secret in the `nuget` environment. Until that secret exists the publish job simply fails, so tagging is safe to try.

```bash
git tag v1.0.0 && git push --tags
```

## How it works

Apple's API is Swift-only (`async`, enums with associated values), so .NET cannot call it directly. `DeclaredAgeRangeWrapper.swift` exposes an `@objc` class `DARAgeRangeService` whose static `requestAgeRange` method wraps the call and reports back through a completion block with plain `NSObject` types and an `NSError` in the `DARAgeRangeErrorDomain`. The bridge is built with a deployment target of iOS 13 / macOS 11 and weak-links `DeclaredAgeRange.framework`, so apps with a lower minimum OS still launch on older systems; the .NET side surfaces `AgeRangeService.IsSupported` for the runtime check.

The .NET project binds those Objective-C classes as `internal` types and layers a small managed API on top: a `TaskCompletionSource` turns the completion block into a `Task`, the call is marshalled to the main thread, `NSNumber` bounds become `int?`, and the `NSError` becomes an `AgeRangeException` with a typed `AgeRangeError`.

## Credits

Started from Alex Soto's [DeclaredAgeRangeSample](https://github.com/dalexsoto/DeclaredAgeRangeSample), which showed the Swift-bridge-plus-binding approach.

## Related links

* [Declared Age Range - Apple Developer Documentation](https://developer.apple.com/documentation/declaredagerange/)
* [Deliver age-appropriate experiences in your app - WWDC 2025](https://developer.apple.com/videos/play/wwdc2025/299/)

## License

MIT. See [LICENSE](LICENSE).
