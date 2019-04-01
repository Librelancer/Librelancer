# Librelancer [![](https://img.shields.io/badge/chat-on%20discord-green.svg)](https://discord.gg/QW2vzxx)  
A re-implementation of the 2003 Space Game [Freelancer](https://en.wikipedia.org/wiki/Freelancer_(video_game)) in C# and OpenGL.

Currently running on Windows and Linux (macOS pending maintainer)
Pull Requests are welcome!

Support Librelancer on Patreon: https://www.patreon.com/librelancer
## Build Status

|  Name | Status |
|-|-|
| Windows AnyCPU | [![Build status](https://ci.appveyor.com/api/projects/status/k55k2t37q1ytm1w3?svg=true)](https://ci.appveyor.com/project/CallumDev/librelancer) |
| Linux amd64 | [![Build Status](https://travis-ci.org/Librelancer/Librelancer.svg?branch=master)](https://travis-ci.org/Librelancer/Librelancer) |

Download daily builds from https://librelancer.net/downloads.html

## General Requirements
* GPU must be capable of OpenGL 3.1+
* A Freelancer installation (Vanilla recommended, some mods may work)

## Build Instructions

### Windows
**Prerequisites:**

* 64-bit Windows 7 or newer
* .NET Framework 4.6+
* Visual Studio 2017
* [CMake](https://cmake.org/)

**Steps:**
1. Clone this repository with all submodules (Visual Studio 2017, Git bash, etc.)
2. Run `build.ps1` in Powershell. (Can be launched from cmd by `powershell -File .\build.ps1`)

Powershell security issues can be troubleshooted [here](https://cakebuild.net/docs/tutorials/powershell-security).




### Linux
*Note for Ubuntu/Debian users: the official mono package must be installed as outlined [here](http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives). The packages in the regular repositories are broken.*

**Prerequisites:**
* Mono 5.x+
* SDL2
* OpenAL
* gcc and g++
* CMake
* Freetype

**Steps:**
1. Clone this repository with `git clone --recursive https://github.com/Librelancer/Librelancer`
2. Run `build.sh`


## Screenshots
See: https://librelancer.net/screenshots/
