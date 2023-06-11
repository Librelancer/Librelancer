cmake_minimum_required (VERSION 2.8.12)
project(win32dialogs)
set(CMAKE_BUILD_TYPE release)
set(CMAKE_CXX_STANDARD 11)
include_directories("../../extern/nativefiledialog/src/include")

if(MSVC_IDE)
set(MANIFEST_FILE dllmanifest.manifest)
else()
set(MANIFEST_FILE manifest.rc)
endif()
add_library(win32dialogs SHARED
  ../../extern/nativefiledialog/src/nfd_common.c 
  ../../extern/nativefiledialog/src/nfd_win.cpp
  win32dialogs.nfd.c
  win32dialogs.win32.c
  ${MANIFEST_FILE}
)
target_link_libraries(win32dialogs comctl32 user32)
