# ================================================================
#  Pousse ! (GameExe) - Installation et mise a jour depuis GitHub
# ================================================================
#  - Telecharge la derniere version publiee (GitHub Releases)
#  - L'installe dans %LOCALAPPDATA%\Programs\GameExe (aucun droit admin)
#  - Cree les raccourcis Bureau + menu Demarrer
#  - Relancer ce script = verifier / faire la mise a jour
#  Parametres : -Uninstall   -NoLaunch
# ================================================================
param([switch]$Uninstall, [switch]$NoLaunch)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$Repo      = 'Snowoo-2z/gameexe'
$ExeName   = 'GameExe.exe'
$InstallDir  = Join-Path $env:LOCALAPPDATA 'Programs\GameExe'
$ExePath     = Join-Path $InstallDir $ExeName
$VersionFile = Join-Path $InstallDir 'installed-version.txt'
$DesktopLnk  = Join-Path ([Environment]::GetFolderPath('Desktop')) 'GameExe.lnk'
$StartLnk    = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\GameExe.lnk'

function Info($msg) { Write-Host $msg -ForegroundColor Cyan }
function Ok($msg)   { Write-Host $msg -ForegroundColor Green }
function Warn($msg) { Write-Host $msg -ForegroundColor Yellow }

# ---------------- Desinstallation ----------------
if ($Uninstall) {
    Info 'Desinstallation de GameExe...'
    Get-Process GameExe -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $InstallDir -ErrorAction SilentlyContinue
    Remove-Item -Force $DesktopLnk -ErrorAction SilentlyContinue
    Remove-Item -Force $StartLnk -ErrorAction SilentlyContinue
    Ok 'Jeu desinstalle. A bientot !'
    return
}

Write-Host ''
Write-Host '  ====================================' -ForegroundColor Green
Write-Host '    POUSSE ! - installation / update  ' -ForegroundColor Green
Write-Host '  ====================================' -ForegroundColor Green
Write-Host ''

try {
    # ---------------- 1. Derniere version sur GitHub ----------------
    Info 'Recherche de la derniere version sur GitHub...'
    $headers = @{ 'User-Agent' = 'GameExe-Installer'; 'Accept' = 'application/vnd.github+json' }
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest" -Headers $headers
    $latest  = ($release.tag_name -replace '^v', '')

    $asset = $release.assets | Where-Object { $_.name -eq $ExeName } | Select-Object -First 1
    if (-not $asset) { throw "L'executable $ExeName est introuvable dans la derniere release." }

    $installed = $null
    if (Test-Path $VersionFile) { $installed = (Get-Content $VersionFile -Raw).Trim() }

    if ((Test-Path $ExePath) -and $installed -eq $latest) {
        Ok "Le jeu est deja a jour (v$latest)."
    }
    else {
        if ($installed) { Info "Mise a jour : v$installed -> v$latest" }
        else { Info "Installation de la v$latest" }

        # ---------------- 2. Telechargement ----------------
        $tmp = Join-Path $env:TEMP 'GameExe_download.exe'
        Info 'Telechargement en cours (quelques secondes)...'
        $wc = New-Object Net.WebClient
        $wc.Headers.Add('User-Agent', 'GameExe-Installer')
        $wc.DownloadFile($asset.browser_download_url, $tmp)
        if ((Get-Item $tmp).Length -lt 1MB) { throw 'Le fichier telecharge semble incomplet. Reessaie.' }

        # ---------------- 3. Installation ----------------
        Get-Process GameExe -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 400
        New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
        Move-Item -Force $tmp $ExePath
        Set-Content -Path $VersionFile -Value $latest -Encoding ASCII
        Ok "Jeu installe dans : $InstallDir"
    }

    # ---------------- 4. Raccourcis ----------------
    $shell = New-Object -ComObject WScript.Shell
    foreach ($lnk in @($DesktopLnk, $StartLnk)) {
        $sc = $shell.CreateShortcut($lnk)
        $sc.TargetPath = $ExePath
        $sc.WorkingDirectory = $InstallDir
        $sc.IconLocation = "$ExePath,0"
        $sc.Description = 'Pousse ! - petit platformer'
        $sc.Save()
    }
    Ok 'Raccourcis crees (Bureau + menu Demarrer).'

    # ---------------- 5. Lancement ----------------
    Write-Host ''
    Ok 'Tout est pret ! Bon jeu !'
    Warn 'Astuce : le jeu verifie aussi les mises a jour tout seul a chaque demarrage.'
    if (-not $NoLaunch) {
        Info 'Lancement du jeu...'
        Start-Process $ExePath -WorkingDirectory $InstallDir
    }
}
catch {
    Write-Host ''
    Write-Host "ERREUR : $($_.Exception.Message)" -ForegroundColor Red
    Warn 'Causes possibles : pas de connexion Internet, ou aucune version publiee pour le moment.'
    Warn "Va voir ici : https://github.com/$Repo/releases"
}
