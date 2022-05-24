#!/bin/sh

SCRIPT_DIR=$(cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd)
SRC_DIR="$SCRIPT_DIR/../src"

docker run --rm --user $UID \
    -w /src -t \
    -v$SRC_DIR:/src \
    -eHOME=/src \
    mcr.microsoft.com/dotnet/sdk:6.0-focal dotnet publish -c Release