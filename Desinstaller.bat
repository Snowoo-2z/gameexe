@echo off
chcp 65001 >nul
title Desinstallation - Pousse ! (GameExe)
setlocal

if exist "%~dp0tools\installer.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\installer.ps1" -Uninstall
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-Item -Recurse -Force \"$env:LOCALAPPDATA\Programs\GameExe\" -ErrorAction SilentlyContinue; Remove-Item -Force \"$env:USERPROFILE\Desktop\GameExe.lnk\" -ErrorAction SilentlyContinue; Remove-Item -Force \"$env:APPDATA\Microsoft\Windows\Start Menu\Programs\GameExe.lnk\" -ErrorAction SilentlyContinue; Write-Host 'Jeu desinstalle. A bientot !' -ForegroundColor Green"
)

echo.
pause
endlocal
