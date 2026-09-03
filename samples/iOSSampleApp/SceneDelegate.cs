using CoreFoundation;
using DeclaredAgeRange;

namespace iOSSampleApp;

[Register("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    [Export("window")] public UIWindow? Window { get; set; }

    UIButton? requestButton;
    UILabel? resultLabel;

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        Window ??= new UIWindow(windowScene);

        var vc = new UIViewController();
        vc.View!.BackgroundColor = UIColor.SystemBackground;

        requestButton = UIButton.FromType(UIButtonType.System);
        requestButton.SetTitle("Request Age Range (16+)", UIControlState.Normal);
        requestButton.TouchUpInside += async (_, _) => await RequestAgeRangeAsync();

        resultLabel = new UILabel
        {
            Text = AgeRangeService.IsSupported
                ? "Tap the button to request the user's age range."
                : "Declared Age Range requires a physical device on iOS 26 or later.\nTapping the button here will report NotAvailable.",
            TextAlignment = UITextAlignment.Center,
            Lines = 0,
        };

        var stackView = new UIStackView(new UIView[] { requestButton, resultLabel })
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Center,
            Spacing = 16,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };
        vc.View.AddSubview(stackView);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            stackView.CenterXAnchor.ConstraintEqualTo(vc.View.CenterXAnchor),
            stackView.CenterYAnchor.ConstraintEqualTo(vc.View.CenterYAnchor),
            stackView.LeadingAnchor.ConstraintGreaterThanOrEqualTo(vc.View.LayoutMarginsGuide.LeadingAnchor),
            stackView.TrailingAnchor.ConstraintLessThanOrEqualTo(vc.View.LayoutMarginsGuide.TrailingAnchor),
        });

        Window.RootViewController = vc;
        Window.MakeKeyAndVisible();

        // `--request-on-launch` triggers the request without a tap, for smoke tests from the command line:
        //   xcrun simctl launch --console booted com.thelostpantheon.DeclaredAgeRangeSample --request-on-launch
        if (NSProcessInfo.ProcessInfo.Arguments.Contains("--request-on-launch"))
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, TimeSpan.FromSeconds(1)),
                () => _ = RequestAgeRangeAsync());
    }

    // This is the whole integration. No view controller or window needs to be passed;
    // the package presents Apple's sheet from the app's current window.
    async Task RequestAgeRangeAsync()
    {
        requestButton!.Enabled = false;
        try
        {
            var response = await AgeRangeService.RequestAgeRangeAsync(ageGate: 16);

            resultLabel!.Text = response switch
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
            resultLabel!.Text = $"Request failed ({ex.Error}): {ex.Message}";
        }
        catch (Exception ex)
        {
            resultLabel!.Text = $"Unexpected error: {ex}";
        }
        finally
        {
            requestButton.Enabled = true;
            Console.WriteLine($"[DeclaredAgeRange] {resultLabel!.Text}");
        }
    }
}
