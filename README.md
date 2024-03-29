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

Powershell security issues can be troubleshooted [here](https://cakebuild.net/docs/tutorials/powershell-security).




### Linux

**Prerequisites:**

* .NET 8.0 SDK
* SDL2
* OpenAL
* gcc and g++
* CMake
* GTK3, Pango and Cairo headers


**Steps:**

1. Clone this repository with `git clone --recursive https://github.com/Librelancer/Librelancer`
2. Run `build.sh`


## Screenshots
See: https://librelancer.net/screenshots.html
