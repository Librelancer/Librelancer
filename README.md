# Librelancer [![](https://img.shields.io/badge/chat-on%20discord-green.svg)](https://discord.gg/QW2vzxx)
A re-implementation of the 2003 Space Game [Freelancer](https://en.wikipedia.org/wiki/Freelancer_(video_game)) in C# and OpenGL.

Currently running on Windows and Linux (macOS pending maintainer)
Pull Requests are welcome!

Support Librelancer on Patreon: https://www.patreon.com/librelancer


Download compiled binaries from https://librelancer.net/downloads.html

## General Requirements
* GPU must be capable of OpenGL 3.1+
* A Freelancer installation (Vanilla recommended, some mods may work)

## Build Instructions

Note for developers: .\build.ps1 or build.sh _must_ be ran before opening the .sln file, as it generates required files for the solution.

### Windows
**Prerequisites:**

* 64-bit Windows 10 or newer
* Visual Studio 2022 with:
* * .NET 8.0 SDK
* *  Desktop C++ Development Workflow
* [CMake](https://cmake.org/)

**Steps:**

1. Clone this repository with all submodules (Visual Studio's Team Explorer, Git bash, etc.)
2. Run `build.ps1` in Powershell. (Can be launched from cmd by `powershell -File .\build.ps1`)

**Troubleshooting**:

If you run into issues with Powershell execution policies, you can bypass them with `powershell -ExecutionPolicy Bypass -File .\build.ps1`

If you have installed both the 32-bit and 64-bit dotnet SDKs, your PATH can be in an invalid state and the build will fail.
This can be checked with `where dotnet.exe` in the command prompt. If it returns output like:

```
> where.exe dotnet
C:\Program Files (x86)\dotnet\dotnet.exe
C:\Program Files\dotnet\dotnet.exe
```

You need to either uninstall the 32-bit dotnet SDK (recommended), or modify your PATH so the 64-bit SDK appears first in the list.

### Linux

**Prerequisites:**

* .NET 8.0 SDK
* SDL2
* openal-soft
* gcc and g++
* CMake
* GTK3, Pango and Cairo headers


**Steps:**

1. Clone this repository with `git clone --recursive https://github.com/Librelancer/Librelancer`
2. Run `build.sh`


## Screenshots
See: https://librelancer.net/screenshots.html
