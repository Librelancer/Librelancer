# LLShaderCompiler

This application is mostly built as a port of [SDL_shadercross](https://github.com/libsdl-org/SDL_shadercross) to .NET. This relies on the dxc *executable* binary being either present on your PATH, or being passed as an argument `--dxc`. A recent spirv-cross-c-shared binary is also required.

In order to easily align with SDL_GPU descriptor set rules, the HLSL comes with `UNIFORM_SPACE` and `TEXTURE_SPACE` compiler macros predefined in order to select the correct descriptor sets.

LLShaderCompiler also supports transpiling to GLSL 140 (used with GL 3.1). This comes with the following limitations.

- `cbuffer` uniform buffers are translated to uniform arrays of _either_ vec4 or ivec4. You may not mix integer and floating point types in a `cbuffer`.
- Storage buffers are supported only as `StructuredBuffer`, `RWStructuredBuffer` is not supported. Storage buffers are emulated using GL uniform buffers in std140 layout. The compiler will error if it detects the buffer's layout does not match between std430 and std140.

Output files are zstd compressed.

NOTE: dxc seems broken on arm64 linux currently when compiling DXIL, so cross-compilation cannot be complete when using arm64 linux.
