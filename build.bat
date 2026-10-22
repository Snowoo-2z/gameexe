@echo off
chcp 65001 >nul
title Build - GameExe.exe
setlocal

rem ============================================================
rem  FABRIQUE LE .EXE LOCALEMENT (pour developper)
rem  Prerequis : .NET SDK 8  ->  https://dotnet.microsoft.com/download
rem  Resultat   : dist\GameExe.exe (autonome, un seul fichier)
rem  Note : GitHub Actions fait exactement la meme chose en
rem  ligne a chaque push, pas besoin de l'installer pour ca.
rem ============================================================

where dotnet >nul 2>&1
if errorlevel 1 (
    echo.
    echo [ERREUR] Le SDK .NET est introuvable.
    echo Installe-le depuis https://dotnet.microsoft.com/download ^(version 8^)
    echo puis relance ce fichier.
    echo.
    pause
    exit /b 1
)

set /p VERSION=<version.txt
echo.
echo === Build de GameExe v%VERSION% (un seul .exe autonome) ===
echo.

dotnet publish src\GameExe\GameExe.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=%VERSION% -p:InformationalVersion=%VERSION% -o dist

if errorlevel 1 (
    echo.
    echo [ECHEC] Le build a echoue, regarde les erreurs ci-dessus.
    pause
    exit /b 1
)

echo.
echo [OK] Executable pret : dist\GameExe.exe
explorer dist
pause
endlocal
