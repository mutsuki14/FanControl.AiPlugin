@echo off
chcp 65001 >nul
echo ============================================
echo   FanControl AI Plugin - Config Tool
echo ============================================
echo.

set "CONFIG_EXE=%~dp0..\Source\ConfigTool\bin\Release\net8.0-windows\FanControl.AiPlugin.ConfigTool.exe"

:: Check if config tool is built
if not exist "%CONFIG_EXE%" (
    echo [INFO] Config tool not built yet. Attempting to build...
    echo.

    dotnet --version >nul 2>&1
    if errorlevel 1 (
        echo [ERROR] .NET SDK not found.
        echo Please install .NET 8.0 SDK or build the config tool manually.
        pause
        exit /b 1
    )

    cd /d "%~dp0..\Source"
    dotnet build ConfigTool\FanControl.AiPlugin.ConfigTool.csproj -c Release
    if errorlevel 1 (
        echo [ERROR] Build failed.
        pause
        exit /b 1
    )
    echo [OK] Build succeeded.
    echo.
)

:: Ask for config file path (optional)
echo You can specify a config file path, or press Enter to use default.
echo Example: C:\Program Files\FanControl\Plugins\ai-fan-settings.json
echo.
set /p "CONFIG_PATH=Config file path (Enter=default): "

echo.
echo Starting config tool...
if "%CONFIG_PATH%"=="" (
    start "" "%CONFIG_EXE%"
) else (
    start "" "%CONFIG_EXE%" "%CONFIG_PATH%"
)
