param(
    [Parameter(Mandatory=$true)]
    [string]$Login,
    [Parameter(Mandatory=$true)]
    [string]$RemoteName
)

$ErrorActionPreference = "Stop"

#
# Wymuszenie odświeżenia pamięci podręcznej DNS/mDNS w systemie Windows
# To jest workaround na taki problem, że poniższy skrypt nie rozwiązuje nazwy np. raspberrypi01,
# podczas, gdy taka nazwa istnieje w sieci i jest możliwe połączenie do niej przez polecenie ssh...
#
Write-Host "Waking up DNS cache for $RemoteName..." -ForegroundColor DarkGray
ping -n 1 $RemoteName | Out-Null

# 1. Znalezienie pliku AppImage
$appImagePath = Get-ChildItem "Velopack\publish\*.AppImage" | Select-Object -First 1 -ExpandProperty FullName
if (-not $appImagePath) { throw "No AppImage found." }
$appImageName = Split-Path $appImagePath -Leaf

$Remote = "$Login@$RemoteName"
$remotePath = "/home/$Login/$appImageName"
$remoteOldPath = "$remotePath.old"

Write-Host "Publishing to $RemoteName (you will be asked for password once)..." -ForegroundColor Cyan

# 2. Pełna sekwencja w jednym połączeniu SSH
$remoteCommand = "sudo systemctl stop ztrboardgame.service; " +
                    "sudo mv -f $remotePath $remoteOldPath 2>/dev/null || true; " +
                    "cat > $remotePath; " +
                    "chmod +x $remotePath; " +
                    "sudo rm -f $remoteOldPath; " +
                    "sudo systemctl start ztrboardgame.service"

Get-Content $appImagePath -AsByteStream -Raw | ssh $Remote $remoteCommand

Write-Host "Publishing completed successfully!" -ForegroundColor Green
