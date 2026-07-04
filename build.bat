@echo off
:: ============================================================================
:: ACTApi — Standalone build + install script (no InnoSetup required)
:: ============================================================================
:: Usage:  build.bat              — build, install, start service
::         build.bat /uninstall   — stop and remove the service only
::         build.bat /buildonly   — just run dotnet publish, no service ops
:: ============================================================================
setlocal enabledelayedexpansion

set "SERVICE_NAME=ACTApi"
set "INSTALL_DIR=%ProgramFiles%\Entech Security\ACT API Bridge"
set "PUBLISH_DIR=bin\Release\net8.0\publish"

:: ---------------------------------------------------------------------------
:: Parse arguments
:: ---------------------------------------------------------------------------
set "MODE=full"
if /I "%~1"=="/uninstall"  set "MODE=uninstall"
if /I "%~1"=="/buildonly"  set "MODE=buildonly"

:: ---------------------------------------------------------------------------
:: UNINSTALL MODE
:: ---------------------------------------------------------------------------
if "%MODE%"=="uninstall" (
    echo === Stopping service ===
    net stop %SERVICE_NAME% 2>nul
    echo === Deleting service ===
    sc.exe delete %SERVICE_NAME% 2>nul
    echo === Uninstall complete ===
    echo.
    echo To remove files: rmdir /s /q "%INSTALL_DIR%"
    exit /b 0
)

:: ---------------------------------------------------------------------------
:: BUILD
:: ---------------------------------------------------------------------------
echo === dotnet publish ===
dotnet publish -c Release -o "%PUBLISH_DIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed with exit code %ERRORLEVEL%
    exit /b 1
)
echo Build succeeded.

if "%MODE%"=="buildonly" (
    echo === Build-only mode: skipping service install ===
    exit /b 0
)

:: ---------------------------------------------------------------------------
:: SERVICE — stop and remove existing
:: ---------------------------------------------------------------------------
echo === Stopping existing service (if any) ===
net stop %SERVICE_NAME% 2>nul
echo === Removing existing service (if any) ===
sc.exe delete %SERVICE_NAME% 2>nul
timeout /t 2 /nobreak >nul

:: ---------------------------------------------------------------------------
:: COPY FILES
:: ---------------------------------------------------------------------------
echo === Creating install directory ===
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo === Copying published files ===
robocopy "%PUBLISH_DIR%" "%INSTALL_DIR%" /E /NP /NFL /NDL
if %ERRORLEVEL% geq 8 (
    echo ERROR: robocopy failed with exit code %ERRORLEVEL%
    exit /b 1
)

echo === Copying wwwroot\verify files ===
robocopy "wwwroot\verify" "%INSTALL_DIR%\wwwroot\verify" /E /NP /NFL /NDL
if %ERRORLEVEL% geq 8 (
    echo ERROR: robocopy verify files failed with exit code %ERRORLEVEL%
    exit /b 1
)

:: ---------------------------------------------------------------------------
:: PRESERVE Settings.json
:: ---------------------------------------------------------------------------
if exist "%INSTALL_DIR%\Settings\Settings.json" (
    echo === Settings.json already exists, preserving ===
) else (
    if not exist "%INSTALL_DIR%\Settings" mkdir "%INSTALL_DIR%\Settings"
    copy /Y "Settings\Settings.json" "%INSTALL_DIR%\Settings\Settings.json" >nul
    echo === Settings.json copied ===
)

:: ---------------------------------------------------------------------------
:: INSTALL + START SERVICE
:: ---------------------------------------------------------------------------
echo === Creating Windows Service ===
sc.exe create %SERVICE_NAME% binPath= "%INSTALL_DIR%\ACTApi.exe" start= auto displayName= "ACT API Bridge"
if %ERRORLEVEL% neq 0 (
    echo ERROR: sc.exe create failed with exit code %ERRORLEVEL%
    exit /b 1
)
sc.exe description %SERVICE_NAME% "RESTful HTTP bridge for ACT Enterprise WCF API"

echo === Starting service ===
net start %SERVICE_NAME%
if %ERRORLEVEL% neq 0 (
    echo WARNING: Service start returned exit code %ERRORLEVEL%
    echo Run: sc.exe query %SERVICE_NAME%  for status
    exit /b 0
)

echo.
echo =============================================
echo   ACTApi installed and running
echo   Install path: %INSTALL_DIR%
echo   Service name: %SERVICE_NAME%
echo =============================================
exit /b 0
