@echo off
IF [%1]==[/rebuild] (GOTO REBUILD)
IF [%1]==[/?] (GOTO USAGE)
IF EXIST slngen\slngen\bin\Debug\slngen.exe (GOTO RUNCMD)
:REBUILD
cd slngen
%WINDIR%\\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
cd ..
:RUNCMD
slngen\slngen\bin\Debug\slngen.exe
EXIT
:USAGE
echo Usage: %0 [/rebuild]