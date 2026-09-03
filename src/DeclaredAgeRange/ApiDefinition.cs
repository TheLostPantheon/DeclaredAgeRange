using System;
using Foundation;
using ObjCRuntime;

#if __IOS__
using UIKit;
using PlatformAnchor = UIKit.UIViewController;
#elif __MACOS__
using AppKit;
using PlatformAnchor = AppKit.NSWindow;
#endif

// Raw bindings for the Objective-C bridge in DeclaredAgeRangeWrapper.xcframework.
// Everything here is internal; the public surface is the managed AgeRangeService API.
namespace DeclaredAgeRange
{
	[Internal]
	[BaseType (typeof (NSObject), Name = "DARAgeRangeService")]
	[DisableDefaultCtor]
	interface DARAgeRangeService
	{
		[Static]
		[Export ("requestAgeRangeWithAgeGate:secondAgeGate:thirdAgeGate:in:completion:")]
		void RequestAgeRange (nint ageGate, [NullAllowed] NSNumber secondAgeGate, [NullAllowed] NSNumber thirdAgeGate, PlatformAnchor anchor, Action<DARAgeRangeResponse, NSError> completion);
	}

	[Internal]
	[BaseType (typeof (NSObject), Name = "DARAgeRange")]
	[DisableDefaultCtor]
	interface DARAgeRange
	{
		[NullAllowed, Export ("lowerBound", ArgumentSemantic.Strong)]
		NSNumber LowerBound { get; }

		[NullAllowed, Export ("upperBound", ArgumentSemantic.Strong)]
		NSNumber UpperBound { get; }

		[Export ("ageRangeDeclaration")]
		DARAgeRangeDeclaration AgeRangeDeclaration { get; }
	}

	[Internal]
	[BaseType (typeof (NSObject), Name = "DARAgeRangeResponse")]
	[DisableDefaultCtor]
	interface DARAgeRangeResponse
	{
		[Export ("type")]
		DARAgeRangeResponseType Type { get; }

		[NullAllowed, Export ("ageRange", ArgumentSemantic.Strong)]
		DARAgeRange AgeRange { get; }
	}
}
