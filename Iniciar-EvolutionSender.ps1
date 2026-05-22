$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$url = "http://localhost:5238"
$healthUrl = "http://localhost:5238"

Get-Process EvolutionSender -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$projectDir*" } |
    Stop-Process -Force

function Test-AppOnline {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $healthUrl -TimeoutSec 2
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 500
    }
    catch {
        return $false
    }
}

if (-not (Test-AppOnline)) {
    $dotnetHome = Join-Path $projectDir ".dotnet-home"

    $arguments = @(
        "-NoExit",
        "-ExecutionPolicy", "Bypass",
        "-Command",
        "cd `"$projectDir`"; `$env:DOTNET_CLI_HOME=`"$dotnetHome`"; `$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --urls http://localhost:5238"
    )

    Start-Process -FilePath "powershell" -ArgumentList $arguments -WindowStyle Minimized

    $started = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        if (Test-AppOnline) {
            $started = $true
            break
        }
    }

    if (-not $started) {
        Write-Host "Nao foi possivel confirmar que o EvolutionSender iniciou."
        Write-Host "Veja a janela minimizada do PowerShell para detalhes."
        Read-Host "Pressione Enter para sair"
        exit 1
    }
}

Start-Process $url
