#!/bin/sh

# Simple build/development environment.
docker run --name nsec --user $UID \
    -w /src -it \
    -v$(pwd):/src \
    -eHOME=/src \
    -eASPNETCORE_ENVIRONMENT=Development \
    -p8443:8443 \
    -p8080:8080 \
    mcr.microsoft.com/dotnet/sdk:6.0

# docker stop nsec
# docker rm nsec
# Start via dotnet run
