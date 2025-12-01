@echo off
REM FMS Log Nexus - Solution Setup
REM Run this script to create the complete solution structure

echo.
echo ===============================================
echo   FMS Log Nexus - Solution Setup
echo ===============================================
echo.

REM Check for PowerShell
where pwsh >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Using PowerShell Core...
    pwsh -ExecutionPolicy Bypass -File "%~dp0Setup-FMSLogNexus.ps1" %*
) else (
    echo Using Windows PowerShell...
    powershell -ExecutionPolicy Bypass -File "%~dp0Setup-FMSLogNexus.ps1" %*
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Setup failed with error code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Setup completed successfully!
pause
