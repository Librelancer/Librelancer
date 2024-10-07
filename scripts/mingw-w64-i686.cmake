# Build with i686-w64-mingw32

set(CMAKE_SYSTEM_NAME Windows)
set(TOOLCHAIN_PREFIX i686-w64-mingw32)
find_program(POSIX_SUFFIX "${TOOLCHAIN_PREFIX}-gcc-posix")
if(NOT ${POSIX_SUFFIX} STREQUAL POSIX_SUFFIX-NOTFOUND)
    message(STATUS "Choosing posix suffixed mingw64")
    set(TOOLCHAIN_SUFFIX -posix)
else()
    message(STATUS "Posix suffixed mingw64 not found, assuming posix")
endif()
# cross compilers to use for C, C++ and Fortran
set(CMAKE_C_COMPILER ${TOOLCHAIN_PREFIX}-gcc${TOOLCHAIN_SUFFIX})
set(CMAKE_CXX_COMPILER ${TOOLCHAIN_PREFIX}-g++${TOOLCHAIN_SUFFIX})
set(CMAKE_Fortran_COMPILER ${TOOLCHAIN_PREFIX}-gfortran${TOOLCHAIN_SUFFIX})
set(CMAKE_RC_COMPILER ${TOOLCHAIN_PREFIX}-windres)
set(DLL_TOOL_COMMAND ${TOOLCHAIN_PREFIX}-dlltool)
# target environment on the build host system
set(CMAKE_FIND_ROOT_PATH /usr/${TOOLCHAIN_PREFIX})
# big projects
set(CMAKE_C_FLAGS_INIT "-Wa,-mbig-obj")
set(CMAKE_CXX_FLAGS_INIT "-Wa,-mbig-obj")
# modify default behavior of FIND_XXX() commands
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY BOTH)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE BOTH)
