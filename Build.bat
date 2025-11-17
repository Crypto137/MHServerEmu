@echo off

set BUILD_DIR=.\src\MHServerEmu\bin\x64\Release\net8.0
set OUTPUT_DIR=.\build

echo ==================
echo     Building...
echo ==================

dotnet build MHServerEmu.sln -c Release

if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo ==================
echo     Copying...
echo ==================

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
robocopy "%BUILD_DIR%" "%OUTPUT_DIR%" *.* /s /xf *.pdb *.xml /np /njs /njh

echo ==================
echo   Build Complete
echo ==================

pause