with import <nixpkgs> { };
pkgs.stdenv.mkDerivation rec {
  name = "dev-env";
  # Build time dependencies
  nativeBuildInputs = with pkgs; [
    pkg-config
    git
    cmake
    dotnetCorePackages.sdk_8_0_4xx
    freetype.dev
    libGL.dev
    SDL2.dev
    pango.dev
    cairo.dev
    gtk3.dev
    glib.dev
    pcre2.dev
    libgcc
    util-linux.dev
    util-linux.lib
    harfbuzz.dev
    icu.dev
    fontconfig.dev
    openal
  ];

  LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [ pkgs.fontconfig pkgs.SDL2 pkgs.gtk3 pkgs.openal ];
}