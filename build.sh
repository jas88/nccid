#!/bin/sh
set -e
dotnet publish nccid/nccid.csproj -o win -r win-x64 -p:PublishSingleFile=true --self-contained true
cd win
zip -9r ../ncc-win.zip .
