cmake_minimum_required (VERSION 3.1)
project(win32dialogs)
set(CMAKE_CXX_STANDARD 11)

# Sources:
# NFD for Linux/Windows, windows has different manifest requirements based on compiler
# for the Win32 crash dialog.
if(WIN32)
if(MSVC_IDE)
set(PLATFORM_FILES dllmanifest.manifest crashdialog_win32.c ../../extern/nativefiledialog-extended/src/nfd_win.cpp)
else()
set(PLATFORM_FILES manifest.rc crashdialog_win32.c ../../extern/nativefiledialog-extended/src/nfd_win.cpp)
endif()
elseif(APPLE)
set(PLATFORM_FILES ../../extern/nativefiledialog-extended/src/nfd_cocoa.m)
else()
set(PLATFORM_FILES ../../extern/nativefiledialog-extended/src/nfd_gtk.cpp)
endif()

add_library(lancerdialogs SHARED
  ${PLATFORM_FILES}
)
target_compile_definitions(lancerdialogs PRIVATE NFD_EXPORT INTERFACE NFD_SHARED)
if(WIN32)
#set up defines for nfd windows compile
target_include_directories(lancerdialogs PRIVATE ../../extern/nativefiledialog-extended/src/include)
target_link_libraries(lancerdialogs comctl32 user32 ole32 uuid shell32)
elseif(APPLE)
find_library(APPKIT_LIBRARY AppKit)
target_include_directories(lancerdialogs PRIVATE ../../extern/nativefiledialog-extended/src/include)
target_link_libraries(lancerdialogs PRIVATE ${APPKIT_LIBRARY})
target_compile_definitions(lancerdialogs PRIVATE NFD_MACOS_ALLOWEDCONTENTTYPES=0)
else()
# Set up defines for nfd linux compile
find_package(PkgConfig REQUIRED)
pkg_check_modules(GTK3 REQUIRED gtk+-3.0)
target_include_directories(lancerdialogs PRIVATE ${GTK3_INCLUDE_DIRS} ../../extern/nativefiledialog-extended/src/include)
target_link_libraries(lancerdialogs PRIVATE ${GTK3_LINK_LIBRARIES})
target_compile_definitions(lancerdialogs PRIVATE NFD_APPEND_EXTENSION)
endif()
