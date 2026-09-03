using System;
using ObjCRuntime;

// Native enums exchanged with the Swift bridge. Values must match DeclaredAgeRangeWrapper.swift.
namespace DeclaredAgeRange
{
    [Native]
    internal enum DARAgeRangeDeclaration : long
    {
        None = 0,
        SelfDeclared = 1,
        GuardianDeclared = 2,
        Confirmed = 3,
        Unknown = 4,
    }

    [Native]
    internal enum DARAgeRangeResponseType : long
    {
        Sharing = 0,
        DeclinedSharing = 1,
    }
}
