@echo off

for %%i in ("*.ini") do flini-reader-test %%~i --csharp > ..\IniTests.%%~ni.cs

