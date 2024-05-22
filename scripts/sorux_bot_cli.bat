@echo off

:: 获取脚本所在的目录路径
set "SORUX_SCRIPT_PATH=%~dp0"

:: 设置工具路径
set "SORUX_TOOL_PATH=%SORUX_SCRIPT_PATH%resources\ILRepack.exe"

:: 设置环境变量
set "SORUX_TOOL_PATH=%SORUX_TOOL_PATH%"

:: 编译项目
dotnet publish

:: 运行项目
dotnet %SORUX_SCRIPT_PATH%publish\SoruxBotPublishCli.dll
