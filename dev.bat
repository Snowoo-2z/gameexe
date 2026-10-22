@echo off
chcp 65001 >nul
title Dev - GameExe
rem Lance le jeu directement depuis le code source (mode developpeur).
rem Prerequis : .NET SDK 8 -> https://dotnet.microsoft.com/download
dotnet run --project src\GameExe\GameExe.csproj -- --no-update
pause
