#!/bin/bash

# Keyfile required
# We use different output filename to satisfy AV
mcs -keyfile:"$1" -delaysign -win32icon:../Editor/LancerEdit/reactor.ico -out:LibrelancerUpdate.exe -target:winexe -sdk:4.5 -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.IO.Compression.dll -r:System.dll -r:System.IO.Compression.FileSystem.dll Program.cs
mv LibrelancerUpdate.exe updater.exe
