#if NET_20

using System;
using System.Runtime.CompilerServices;
namespace System
{
    public delegate void Action<in T1, in T2>(T1 arg1, T2 arg2);
}

#endif
