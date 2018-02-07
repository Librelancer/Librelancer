# Librelancer [![](https://img.shields.io/badge/chat-on%20discord-green.svg)](https://discord.gg/QW2vzxx)  
A re-implementation of the 2003 Space Game [Freelancer](https://en.wikipedia.org/wiki/Freelancer_(video_game)) in C# and OpenGL.

Currently running on Windows and Linux (macOS pending maintainer)
Pull Requests are welcome!

### Build Status

|  Name | Status |
|-|-|
| Windows AnyCPU | [![Build status](https://ci.appveyor.com/api/projects/status/k55k2t37q1ytm1w3?svg=true)](https://ci.appveyor.com/project/CallumDev/librelancer) |
| Linux amd64 | [![Build Status](https://travis-ci.org/Librelancer/Librelancer.svg?branch=master)](https://travis-ci.org/Librelancer/Librelancer) |

Download daily builds from https://librelancer.net/downloads.html

### General Requirements
* GPU must be capable of OpenGL 3.1+
* A Freelancer installation (Vanilla recommended, some mods may work)

### Build Instructions

#### Windows
*Note:* SDL2, OpenAL-Soft and Freetype for windows are included in this repository.

1. Make sure you have the .NET Framework 4.5 installed with Visual Studio 2017 (optional 2015 for rebuilding cimgui)
2. Clone this repository _and submodules_ with whichever client you choose
3. Run slngen.bat in the repository root
4. Use [CMake](https://cmake.org) to build libbulletc.dll from the source directory extern/BulletSharpPInvoke/libbulletc and copy to bin/Debug(Release)
5. Restore nuget packages (Visual Studio does this automatically)
6. Build src/LibreLancer.Windows.sln, and launch *Launcher*

#### Linux
*Note for Ubuntu/Debian users: the official mono package must be installed as outlined [here](http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives). The packages in the regular repositories are broken.*
1. Install mpv, mono, sdl2, openal, nuget, g++, cmake and freetype
2. Clone this repository with `git clone --recursive https://github.com/Librelancer/Librelancer`
3. Run slngen.unix in the repository root (Requires command line nuget for first run)
4. Run `build.natives.unix` to produce libcimgui.so and libbulletc.so
5. Restore nuget packages (MonoDevelop does this automatically)
6. Build src/LibreLancer.Linux.sln in MonoDevelop or with xbuild/msbulid, and launch *Launcher*


#### Mac
__Mac support is currently broken pending a maintainer__

### Screenshots
See: http://librelancer.github.io/screenshots
