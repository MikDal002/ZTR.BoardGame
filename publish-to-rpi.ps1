param(
    [Parameter(Mandatory=$true)]
    [string]$Login,
    [Parameter(Mandatory=$true)]
    [string]$RemoteName
)

$ErrorActionPreference = "Stop"

$cred = Get-Credential -UserName $Login -Message "Podaj haslo dla $RemoteName"
$session = New-SSHSession -ComputerName $RemoteName -Credential $cred -Verbose

if ($null -eq $session) {
    throw "Nie udalo sie nawiazac polaczenia SSH do $RemoteName."
}

try {
    $sid = $session.SessionId
    $appImageName = "ZtrBoardGame.Console-linux-arm64-alpha.AppImage"
    $remotePath = "/home/$Login/$appImageName"
    $remoteOldPath = "$remotePath.old"

    Write-Host "Zatrzymywanie uslugi i zwalnianie blokad na $RemoteName..." -ForegroundColor Cyan
    Invoke-SSHCommand -SessionId $sid -Command "sudo systemctl stop ztrboardgame.service && sudo fuser -k $remotePath || true" -Verbose

    Write-Host "Przenoszenie starej wersji..." -ForegroundColor Cyan
    Invoke-SSHCommand -SessionId $sid -Command "sudo mv -f $remotePath $remoteOldPath || true" -Verbose

    Write-Host "Przesylanie nowego AppImage..." -ForegroundColor Cyan
    Set-SCPItem -ComputerName $RemoteName -Credential $cred -Path "Velopack\publish\$appImageName" -Destination "/home/$Login" -Force -Verbose

    Write-Host "Nadawanie uprawnien i sprzatanie..." -ForegroundColor Cyan
    Invoke-SSHCommand -SessionId $sid -Command "chmod +x $remotePath && sudo rm -f $remoteOldPath" -Verbose

    Write-Host "Restartowanie uslugi..." -ForegroundColor Cyan
    Invoke-SSHCommand -SessionId $sid -Command "sudo systemctl start ztrboardgame.service" -Verbose

    Write-Host "Publikacja zakonczona sukcesem!" -ForegroundColor Green
}
catch {
    Write-Error "Wystapil blad podczas publikacji: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($session) {
        Remove-SSHSession -SessionId $session.SessionId -Verbose
    }
}
