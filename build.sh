#!/bin/sh
set -e
dotnet publish nccid/nccid.csproj -o win -r win-x64 -p:PublishSingleFile=true --self-contained true
zip -9jr ncc-win.zip win
