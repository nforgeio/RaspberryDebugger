@echo on
REM Configures the environment variables required to build RaspberryDebugger projects.
REM 
REM 	buildenv [ <source folder> ]
REM
REM Note that <source folder> defaults to the folder holding this
REM batch file.
REM
REM This must be [RUN AS ADMINISTRATOR].

REM Default RDBG_ROOT to the folder holding this batch file after stripping
REM off the trailing backslash.

set RDBG_ROOT=%~dp0 
set RDBG_ROOT=%RDBG_ROOT:~0,-2%

if not [%1]==[] set RDBG_ROOT=%1

if exist %RDBG_ROOT%\RaspberryDebugger.sln goto goodPath
echo The [%RDBG_ROOT%\RaspberryDebugger.sln] file does not exist.  Please pass the path
echo to the RaspberryDebugger solution folder.
goto done

:goodPath 

REM Configure the environment variables.

set RDBG_TOOLBIN=%RDBG_ROOT%\ToolBin
set RDBG_BUILD=%RDBG_ROOT%\Build
set RDBG_TEMP=C:\Temp

REM Persist the environment variables.

setx RDBG_ROOT "%RDBG_ROOT%" /M
setx RDBG_TOOLBIN "%RDBG_TOOLBIN%" /M
setx RDBG_BUILD "%RDBG_BUILD%" /M
setx RDBG_TEMP "%RDBG_TEMP%" /M
setx DOTNET_CLI_TELEMETRY_OPTOUT 1 /M

REM Make sure required folders exist.

if not exist "%RDBG_TEMP%" mkdir "%RDBG_TEMP%"
if not exist "%RDBG_TOOLBIN%" mkdir "%RDBG_TOOLBIN%"
if not exist "%RDBG_BUILD%" mkdir "%RDBG_BUILD%"

:done
@echo "========================================================================================="
@echo "Be sure to close and reopen Visual Studio and any command windows to pick up the changes."
@echo "========================================================================================="
pause
