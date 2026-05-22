$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Get-ContainerJson {
    docker ps -a --format "{{json .}}" | ForEach-Object { $_ | ConvertFrom-Json }
}

Write-Step "Verificando acesso ao Docker"
docker version | Out-Null

Write-Step "Procurando container da Evolution"
$containers = @(Get-ContainerJson)
$candidates = @(
    $containers | Where-Object {
        $_.Image -match "evolution" -or
        $_.Names -match "evolution" -or
        $_.Ports -match "8080"
    }
)

if ($candidates.Count -eq 0) {
    throw "Nao encontrei container da Evolution. Abra o Docker Desktop e confira o nome do container."
}

$container = $candidates | Sort-Object {
    if ($_.Image -match "evolution" -or $_.Names -match "evolution") { 0 } else { 1 }
} | Select-Object -First 1

Write-Host "Container selecionado: $($container.Names)"
Write-Host "Imagem atual: $($container.Image)"
Write-Host "Status: $($container.Status)"

$inspect = docker inspect $container.ID | ConvertFrom-Json
$labels = $inspect[0].Config.Labels
$image = $inspect[0].Config.Image

Write-Step "Baixando imagem mais nova"
docker pull $image

$composeProject = $labels."com.docker.compose.project"
$composeService = $labels."com.docker.compose.service"
$composeFiles = $labels."com.docker.compose.project.config_files"
$composeWorkingDir = $labels."com.docker.compose.project.working_dir"

if ($composeProject -and $composeService -and $composeFiles) {
    Write-Step "Container criado por Docker Compose. Atualizando apenas o servico $composeService"

    $composeArgs = @()
    foreach ($file in $composeFiles -split ",") {
        if (-not [string]::IsNullOrWhiteSpace($file)) {
            $composeArgs += "-f"
            $composeArgs += $file
        }
    }

    Push-Location $composeWorkingDir
    try {
        docker compose @composeArgs -p $composeProject pull $composeService
        docker compose @composeArgs -p $composeProject up -d --no-deps $composeService
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Step "Container sem metadados de Docker Compose"
    Write-Host "A imagem foi baixada, mas nao vou remover/recriar o container automaticamente sem compose,"
    Write-Host "porque isso pode perder portas, variaveis ou volumes se o container foi criado manualmente."
    Write-Host ""
    Write-Host "Container: $($container.Names)"
    Write-Host "Imagem: $image"
    Write-Host ""
    Write-Host "Use o Docker Desktop para recriar esse container com a imagem atualizada,"
    Write-Host "ou me passe o docker-compose.yml usado para eu automatizar com seguranca."
}

Write-Step "Resultado"
docker ps -a --filter "id=$($container.ID)" --format "table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}"

Write-Host ""
Write-Host "Atualizacao finalizada."
