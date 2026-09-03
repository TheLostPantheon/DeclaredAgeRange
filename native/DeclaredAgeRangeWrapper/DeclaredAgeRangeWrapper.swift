//
//  DeclaredAgeRangeWrapper.swift
//
//  Objective-C bridge over Apple's Swift-only DeclaredAgeRange framework so it
//  can be consumed from .NET. Type names mirror Apple's API with a `DAR` prefix
//  on the Objective-C side; the .NET package re-exposes them without the prefix.
//

import Foundation
import DeclaredAgeRange

#if canImport(UIKit)
import UIKit
public typealias DARPlatformAnchor = UIViewController
#endif

#if canImport(AppKit) && !targetEnvironment(macCatalyst)
import AppKit
public typealias DARPlatformAnchor = NSWindow
#endif

/// Mirrors `AgeRangeService.AgeRangeDeclaration`. `none` means Apple returned nil.
@objc(DARAgeRangeDeclaration)
public enum DARAgeRangeDeclaration: Int {
    case none = 0
    case selfDeclared = 1
    case guardianDeclared = 2
    case confirmed = 3
    case unknown = 4
}

/// Mirrors the two cases of `AgeRangeService.Response`.
@objc(DARAgeRangeResponseType)
public enum DARAgeRangeResponseType: Int {
    case sharing = 0
    case declinedSharing = 1
}

/// Error codes surfaced to .NET in the `DARAgeRangeErrorDomain` domain. Values must match `AgeRangeError` in C#.
@objc(DARAgeRangeErrorCode)
public enum DARAgeRangeErrorCode: Int {
    case unknown = 0
    case notAvailable = 1
    case invalidRequest = 2
    case invalidAccount = 3
    case declinedOnboarding = 4
    case network = 5
}

public let DARAgeRangeErrorDomain = "DARAgeRangeErrorDomain"

/// Mirrors `AgeRangeService.AgeRange`.
@objc(DARAgeRange)
public class DARAgeRange: NSObject {
    @objc public let lowerBound: NSNumber?
    @objc public let upperBound: NSNumber?
    @objc public let ageRangeDeclaration: DARAgeRangeDeclaration

    @objc public init(lowerBound: NSNumber?, upperBound: NSNumber?, ageRangeDeclaration: DARAgeRangeDeclaration) {
        self.lowerBound = lowerBound
        self.upperBound = upperBound
        self.ageRangeDeclaration = ageRangeDeclaration
    }
}

/// Mirrors `AgeRangeService.Response`.
@objc(DARAgeRangeResponse)
public class DARAgeRangeResponse: NSObject {
    @objc public let type: DARAgeRangeResponseType
    @objc public let ageRange: DARAgeRange?

    @objc public init(type: DARAgeRangeResponseType, ageRange: DARAgeRange?) {
        self.type = type
        self.ageRange = ageRange
    }
}

/// Mirrors `AgeRangeService`.
@objc(DARAgeRangeService)
public class DARAgeRangeService: NSObject {

    /// Objective-C bridge for `AgeRangeService.shared.requestAgeRange(ageGates:in:)`.
    /// - Parameters:
    ///   - ageGate: The required minimum age for your app.
    ///   - secondAgeGate: An optional additional age threshold. Pass nil to omit.
    ///   - thirdAgeGate: An optional additional age threshold. Pass nil to omit.
    ///   - anchor: The view controller (iOS) or window (macOS) to present system UI from.
    ///   - completion: Called on the main queue with a response or an NSError in `DARAgeRangeErrorDomain`.
    @objc public static func requestAgeRange(ageGate: Int,
                                             secondAgeGate: NSNumber?,
                                             thirdAgeGate: NSNumber?,
                                             in anchor: DARPlatformAnchor,
                                             completion: @escaping (DARAgeRangeResponse?, NSError?) -> Void) {
        guard #available(iOS 26.0, macOS 26.0, *) else {
            completion(nil, makeError(.notAvailable, underlying: nil))
            return
        }

        Task { @MainActor in
            do {
                let response = try await AgeRangeService.shared.requestAgeRange(
                    ageGates: ageGate, secondAgeGate?.intValue, thirdAgeGate?.intValue, in: anchor)

                switch response {
                case .sharing(let swiftRange):
                    let range = DARAgeRange(
                        lowerBound: swiftRange.lowerBound as NSNumber?,
                        upperBound: swiftRange.upperBound as NSNumber?,
                        ageRangeDeclaration: mapDeclaration(swiftRange.ageRangeDeclaration))
                    completion(DARAgeRangeResponse(type: .sharing, ageRange: range), nil)
                case .declinedSharing:
                    completion(DARAgeRangeResponse(type: .declinedSharing, ageRange: nil), nil)
                @unknown default:
                    // Future-proof fallback: treat unknown responses as a safe decline.
                    completion(DARAgeRangeResponse(type: .declinedSharing, ageRange: nil), nil)
                }
            } catch {
                completion(nil, mapError(error))
            }
        }
    }

    @available(iOS 26.0, macOS 26.0, *)
    private static func mapDeclaration(_ declaration: AgeRangeService.AgeRangeDeclaration?) -> DARAgeRangeDeclaration {
        guard let declaration else { return .none }
        switch declaration {
        case .selfDeclared: return .selfDeclared
        case .guardianDeclared: return .guardianDeclared
        default:
            // `confirmed` was added in OS 26.5 and supersedes the deprecated *Checked cases (26.2).
            if #available(iOS 26.5, macOS 26.5, *), declaration == .confirmed {
                return .confirmed
            }
            return .unknown
        }
    }

    @available(iOS 26.0, macOS 26.0, *)
    private static func mapError(_ error: Swift.Error) -> NSError {
        var code: DARAgeRangeErrorCode = .unknown
        if let serviceError = error as? AgeRangeService.Error {
            switch serviceError {
            case .notAvailable: code = .notAvailable
            case .invalidRequest: code = .invalidRequest
            @unknown default:
                // Cases documented by Apple but not present in every SDK; match by name so a
                // wrapper built with an older SDK still classifies them on a newer OS.
                switch String(describing: serviceError) {
                case "invalidAccount": code = .invalidAccount
                case "declinedOnboarding": code = .declinedOnboarding
                case "network": code = .network
                default: break
                }
            }
        }
        return makeError(code, underlying: error as NSError)
    }

    /// Apple's `AgeRangeService.Error` carries no description, so supply actionable ones.
    private static func makeError(_ code: DARAgeRangeErrorCode, underlying: NSError?) -> NSError {
        let message: String
        switch code {
        case .notAvailable:
            message = "Declared Age Range is not available. It requires a physical device running iOS 26 or macOS 26 " +
                      "and an app signed with the com.apple.developer.declared-age-range entitlement; the iOS Simulator always reports this."
        case .invalidRequest:
            message = "The age range request was invalid. Check the age gates and that the app has the " +
                      "com.apple.developer.declared-age-range entitlement enabled for its App ID."
        case .invalidAccount:
            message = "The current Apple Account is not eligible for age range sharing."
        case .declinedOnboarding:
            message = "The person declined the age range onboarding flow."
        case .network:
            message = "A network or server issue prevented completing the age range request."
        case .unknown:
            message = underlying?.localizedDescription ?? "The age range request failed."
        }

        var userInfo: [String: Any] = [NSLocalizedDescriptionKey: message]
        if let underlying {
            userInfo[NSUnderlyingErrorKey] = underlying
        }
        return NSError(domain: DARAgeRangeErrorDomain, code: code.rawValue, userInfo: userInfo)
    }
}
