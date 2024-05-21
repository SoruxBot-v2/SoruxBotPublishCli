#!/bin/bash

SORUX_SCRIPT_PATH=$(cd "$(dirname "$0")"; pwd)

SORUX_TOOL_PATH=${SORUX_SCRIPT_PATH}/resources/ILRepack.exe

export SORUX_TOOL_PATH



dotnet build -c Release

dotnet run ${SORUX_SCRIPT_PATH}/Release/net8.0/SoruxShadeDll

