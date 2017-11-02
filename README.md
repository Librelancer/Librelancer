# Librelancer
A re-implementation of the 2003 Space Game [Freelancer](https://en.wikipedia.org/wiki/Freelancer_(video_game)) in C# and OpenGL.

Currently running on Windows and Linux (macOS pending maintainer)
Pull Requests are welcome!

To help speed up development, donations can be sent to paypal.paymentmc@gmail.com.

### General Requirements
* GPU must be capable of OpenGL 3.1+
* A Freelancer installation (Vanilla recommended, some mods may work)

### Build Instructions

#### Windows
*Note:* SDL2, OpenAL-Soft and Freetype for windows are included in this repository.

1. Make sure you have the .NET Framework 4.5 installed with Visual Studio 2015
2. Clone this repository _and submodules_ with whichever client you choose
3. Run slngen.bat in the repository root
4. Restore nuget packages (Visual Studio does this automatically)
5. Build src/LibreLancer.Linux.sln, and launch *Launcher*

#### Linux
*Note for Ubuntu users: the official mono package must be installed as outlined [here](http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives)*
1. Install mpv, mono, sdl2, openal, nuget and freetype
2. Clone this repository with `git clone --recursive https://github.com/CallumDev/Librelancer`
3. Run slngen.unix in the repository root (Requires command line nuget for first run)
4. Restore nuget packages (MonoDevelop does this automatically)
5. Build src/LibreLancer.Linux.sln in MonoDevelop or with xbuild, and launch *Launcher*


#### Mac
__Mac support is currently broken pending a maintainer__

### Screenshots
See: http://callumdev.github.io/Librelancer/screenshots
