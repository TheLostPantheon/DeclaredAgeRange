using CoreFoundation;
using DeclaredAgeRange;

namespace macOSSampleApp;

[Register("AppDelegate")]
public class AppDelegate : NSApplicationDelegate
{
    NSWindow? window;
    NSButton? requestButton;
    NSTextField? resultLabel;

    public override void DidFinishLaunching(NSNotification notification)
    {
        requestButton = NSButton.CreateButton("Request Age Range (16+)", () => _ = RequestAgeRangeAsync());

        resultLabel = NSTextField.CreateWrappingLabel(AgeRangeService.IsSupported
            ? "Click the button to request the user's age range."
            : "Declared Age Range requires macOS 26 or later.");
        resultLabel.Alignment = NSTextAlignment.Center;

        var stack = NSStackView.FromViews(new NSView[] { requestButton, resultLabel });
        stack.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
        stack.Spacing = 16;
        stack.EdgeInsets = new NSEdgeInsets(24, 24, 24, 24);

        window = new NSWindow(
            new CGRect(0, 0, 480, 200),
            NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable,
            NSBackingStore.Buffered,
            deferCreation: false)
        {
            Title = "DeclaredAgeRange",
            ContentView = stack,
        };
        window.Center();
        window.MakeKeyAndOrderFront(null);

        // `--request-on-launch` triggers the request without a click, for smoke tests from the command line.
        if (NSProcessInfo.ProcessInfo.Arguments.Contains("--request-on-launch"))
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromSeconds(1)),
                () => _ = RequestAgeRangeAsync());
    }

    // This is the whole integration. No window needs to be passed;
    // the package presents Apple's sheet from the app's key window.
    async Task RequestAgeRangeAsync()
    {
        requestButton!.Enabled = false;
        Console.WriteLine("[DeclaredAgeRange] requesting (IsSupported=" + AgeRangeService.IsSupported + ")...");
        try
        {
            var response = await AgeRangeService.RequestAgeRangeAsync(ageGate: 16);

            resultLabel!.StringValue = response switch
            {
                AgeRangeResponse.Sharing { AgeRange: var range } =>
                    $"Age range: {range}\n" +
                    (range.IsAtLeast(16) ? "User is 16 or older: show the full experience."
                                         : "User is under 16: show the restricted experience."),
                _ => "User declined to share their age range.",
            };
        }
        catch (AgeRangeException ex)
        {
            resultLabel!.StringValue = $"Request failed ({ex.Error}): {ex.Message}";
        }
        catch (Exception ex)
        {
            resultLabel!.StringValue = $"Unexpected error: {ex}";
        }
        finally
        {
            requestButton.Enabled = true;
            Console.WriteLine($"[DeclaredAgeRange] {resultLabel!.StringValue}");
        }
    }

    public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => true;
}
