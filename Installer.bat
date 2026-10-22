@echo off
chcp 65001 >nul
title Installation / mise a jour - Pousse ! (GameExe)
setlocal

rem ============================================================
rem  INSTALLEUR + MISE A JOUR AUTOMATIQUE (depuis GitHub)
rem  ----------------------------------------------------------
rem  Double-clique sur ce fichier :
rem    - 1ere fois  : installe le jeu + raccourci sur le Bureau
rem    - fois suivantes : met le jeu a jour si besoin
rem  Le script d'installation est toujours recupere a jour
rem  depuis le depot GitHub (sauf si le dossier tools\ existe
rem  a cote, ex. dans un clone du repo).
rem ============================================================

if exist "%~dp0tools\installer.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\installer.ps1" %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "try { iex ((New-Object Net.WebClient).DownloadString('https://raw.githubusercontent.com/Snowoo-2z/gameexe/main/tools/installer.ps1')) } catch { Write-Host ''; Write-Host 'ERREUR : impossible de contacter GitHub. Verifie ta connexion Internet.' -ForegroundColor Red }"
)

echo.
pause
endlocal
