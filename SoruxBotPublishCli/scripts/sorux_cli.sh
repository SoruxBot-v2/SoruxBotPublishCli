#!/bin/bash

SCRIPT_PATH=$(cd "$(dirname "$0")"; pwd)
dotnet build -c Release
#echo $SCRIPT_PATH
dotnet run $(SCRIPT_PATH)/Release/net8.0/SoruxShadeDll

