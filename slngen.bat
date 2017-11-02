@echo off
IF [%1]==[/rebuild] (GOTO REBUILD)
IF [%1]==[/?] (GOTO USAGE)
IF EXIST slngen\slngen\bin\Debug\slngen.exe (GOTO RUNCMD)
:REBUILD
cd slngen
IF EXIST nuget.exe (goto RESTORE_PKG)
echo Downloading nuget.exe
powershell -Command "(New-Object Net.WebClient).DownloadFile('https://dist.nuget.org/win-x86-commandline/latest/nuget.exe', 'nuget.exe')"
:RESTORE_PKG
nuget restore
%WINDIR%\\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
cd ..
:RUNCMD
slngen\slngen\bin\Debug\slngen.exe
PAUSE
EXIT
:USAGE
echo Usage: %0 [/rebuild]