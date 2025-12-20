using System;
using System.Diagnostics;
// ReSharper disable CheckNamespace

namespace JetBrains.Annotations;

[Conditional("JETBRAINS_ANNOTATIONS")]
class MeansImplicitUseAttribute : Attribute
{
}
