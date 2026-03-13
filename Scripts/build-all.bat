@echo off
chcp 65001 >nul
echo ============================================
echo   FanControl AI Plugin - Build Script
echo ============================================
echo.

:: Check .NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found.
    echo Please install .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/3] Building plugin (with LibreHardwareMonitor)...
cd /d "%~dp0..\Source"
dotnet build FanControl.AiPlugin.csproj -c Release -p:USE_LHM=true
if errorlevel 1 (
    echo [ERROR] Plugin build failed.
    pause
    exit /b 1
)
echo [OK] Plugin build succeeded.
echo.

echo [2/3] Building config tool...
dotnet build ConfigTool\FanControl.AiPlugin.ConfigTool.csproj -c Release
if errorlevel 1 (
    echo [ERROR] Config tool build failed.
    pause
    exit /b 1
)
echo [OK] Config tool build succeeded.
echo.

echo [3/3] Building demo...
dotnet build Demo\FanControl.AiPlugin.Demo.csproj -c Release -p:USE_LHM=true
if errorlevel 1 (
    echo [WARN] Demo build failed (non-critical).
) else (
    echo [OK] Demo build succeeded.
)

echo.
echo ============================================
echo   Build complete!
echo ============================================
echo.
echo Plugin DLL:     Source\bin\Release\net8.0\FanControl.AiPlugin.dll
echo Config Tool:    Source\ConfigTool\bin\Release\net8.0-windows\FanControl.AiPlugin.ConfigTool.exe
echo Demo:           Source\Demo\bin\Release\net8.0\FanControl.AiPlugin.Demo.exe
echo.
echo Next: Run deploy-plugin.bat to copy files to FanControl.
echo.
pause
