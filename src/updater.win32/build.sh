#!/bin/bash

mcs -out:updater.exe -target:winexe -sdk:4.5 -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.IO.Compression.dll -r:System.dll -r:System.IO.Compression.FileSystem.dll Program.cs

