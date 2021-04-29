#!/bin/sh
set -e
dotnet publish -o win -r win-x64 -p:PublishSingleFile=true --self-contained true
zip -9jr ncc-win.zip win
