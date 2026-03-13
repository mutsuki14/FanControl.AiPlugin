@echo off
chcp 65001 >nul
echo ============================================
echo   FanControl AI Plugin - Deploy Script
echo ============================================
echo.

:: Default FanControl path
set "FANCONTROL_DIR=C:\Program Files\FanControl"
set "PLUGINS_DIR=%FANCONTROL_DIR%\Plugins"
set "BUILD_DIR=%~dp0..\Source\bin\Release\net8.0"

:: Check if build exists
if not exist "%BUILD_DIR%\FanControl.AiPlugin.dll" (
    echo [ERROR] Plugin DLL not found at: %BUILD_DIR%
    echo Please run build-all.bat first.
    pause
    exit /b 1
)

:: Ask user for FanControl path
echo Current FanControl path: %FANCONTROL_DIR%
set /p "CUSTOM_DIR=Press Enter to use default, or type custom path: "
if not "%CUSTOM_DIR%"=="" (
    set "FANCONTROL_DIR=%CUSTOM_DIR%"
    set "PLUGINS_DIR=%CUSTOM_DIR%\Plugins"
)

:: Check FanControl directory
if not exist "%FANCONTROL_DIR%" (
    echo [ERROR] FanControl directory not found: %FANCONTROL_DIR%
    echo Please check the path and try again.
    pause
    exit /b 1
)

:: Create Plugins dir if needed
if not exist "%PLUGINS_DIR%" (
    echo Creating Plugins directory...
    mkdir "%PLUGINS_DIR%"
)

echo.
echo Deploying to: %PLUGINS_DIR%
echo.

:: Copy plugin DLL
echo [1/4] Copying FanControl.AiPlugin.dll...
copy /Y "%BUILD_DIR%\FanControl.AiPlugin.dll" "%PLUGINS_DIR%\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy plugin DLL. Try running as Administrator.
    pause
    exit /b 1
)

:: Copy LHM libraries (if exist)
echo [2/4] Copying LibreHardwareMonitor libraries...
if exist "%BUILD_DIR%\LibreHardwareMonitorLib.dll" (
    copy /Y "%BUILD_DIR%\LibreHardwareMonitorLib.dll" "%PLUGINS_DIR%\" >nul
)
if exist "%BUILD_DIR%\HidSharp.dll" (
    copy /Y "%BUILD_DIR%\HidSharp.dll" "%PLUGINS_DIR%\" >nul
)

:: Copy config file (only if not exists - don't overwrite user config)
echo [3/4] Copying default config...
if not exist "%PLUGINS_DIR%\ai-fan-settings.json" (
    copy /Y "%~dp0..\Plugin\ai-fan-settings.json" "%PLUGINS_DIR%\" >nul
    echo     Default config copied. Please edit ai-fan-settings.json with your API key.
) else (
    echo     Config file already exists, skipping (won't overwrite your settings).
)

echo [4/4] Done!
echo.
echo ============================================
echo   Deployment complete!
echo ============================================
echo.
echo Files deployed to: %PLUGINS_DIR%
echo.
echo Next steps:
echo   1. Edit %PLUGINS_DIR%\ai-fan-settings.json
echo      - Set your apiKey
echo      - Set your endpointUrl
echo      - Set sensorProvider to "lhm"
echo   2. Restart FanControl (run as Administrator)
echo.
pause
