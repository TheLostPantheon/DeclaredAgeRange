#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace DeclaredAgeRange;

/// <summary>Finds the UI element Apple's age range sheet should be presented from. Must run on the main thread.</summary>
internal static class PlatformAnchorResolver
{
#if __IOS__
    public static UIViewController? Resolve()
    {
        var app = UIApplication.SharedApplication;

        var scenes = app.ConnectedScenes.OfType<UIWindowScene>().ToList();
        var scene = scenes.FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive)
                    ?? scenes.FirstOrDefault();

        var window = scene?.Windows.FirstOrDefault(w => w.IsKeyWindow)
                     ?? scene?.Windows.FirstOrDefault();

#pragma warning disable CA1422 // UIApplication.Windows is deprecated but is the only fallback for apps without scenes.
        window ??= app.Windows.FirstOrDefault(w => w.IsKeyWindow) ?? app.Windows.FirstOrDefault();
#pragma warning restore CA1422

        var controller = window?.RootViewController;
        while (controller?.PresentedViewController is { } presented)
            controller = presented;

        return controller;
    }
#elif __MACOS__
    public static NSWindow? Resolve()
    {
        var app = NSApplication.SharedApplication;
        if (app.KeyWindow is { } key) return key;
        if (app.MainWindow is { } main) return main;
        var windows = app.DangerousWindows;
        return windows.Count > 0 ? windows[0] : null;
    }
#endif
}
