@echo off
chcp 65001 >nul
echo ============================================
echo   FanControl AI Plugin - Demo Console
echo ============================================
echo.
echo This demo runs the AI fan control logic in a standalone console.
echo Useful for testing AI connectivity and sensor readings.
echo.
echo NOTE: Must run as Administrator for real sensor access.
echo.

set "DEMO_DIR=%~dp0..\Source\Demo"

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found.
    pause
    exit /b 1
)

cd /d "%DEMO_DIR%"

echo Running demo with LibreHardwareMonitor sensors...
echo Press Ctrl+C to stop.
echo.
dotnet run -c Release -p:USE_LHM=true
pause
