C code is compiled using the T4 template SSEMath.tt
This only works on macOS, and Linux with gcc
Cygwin support is planned. MSVC++ will not be supported due to a lack of the __attribute__((sysvabi)) feature
Only x86/x86_64 is supported for now. Other architectures will fall back to a scalar pipeline

To compile (Xamarin Studio)
Right-click SSEMath.tt, Tools->Process T4 Template

RULES:
- Each C function must be in a separate file
- Each C file must start with a comment defining the C# prototype
- Each C file must include embedding.h, and each function must use the EMBED macro 
- Each C file must be added to sources.txt
- Each C file must only have one function, and cannot call external functions (including standard lib). The files are NOT linked.

Example file:

//int addition(int a, int b);
#include "embedding.h"
int EMBED the_c_function(int a, int b)
{
	return a + b;
}