// StructsAndEnums.cs marks its enums with ObjCRuntime.[Native]. That attribute only affects
// binding-generator output, so a no-op stand-in lets the file compile on plain .NET.
namespace ObjCRuntime;

[AttributeUsage(AttributeTargets.Enum)]
internal sealed class NativeAttribute : Attribute
{
}
