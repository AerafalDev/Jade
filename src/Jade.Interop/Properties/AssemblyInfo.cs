using System.Runtime.CompilerServices;

// The runtime never marshals for this assembly, so every native signature has to be blittable:
// calls stay zero-cost and behave the same under the JIT and NativeAOT.
[assembly: DisableRuntimeMarshalling]
