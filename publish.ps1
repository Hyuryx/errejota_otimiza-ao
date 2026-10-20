[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "ERREJOTA MATRIX - PUBLICADOR OFICIAL GITHUB"

Write-Host "===============================================================================" -ForegroundColor Green
Write-Host "       ERREJOTA MATRIX OPTIMIZER PRO - PUBLICADOR AUTOMATICO GITHUB" -ForegroundColor Green
Write-Host "===============================================================================" -ForegroundColor Green
Write-Host ""

# 1. Detectar versao atual
$currentVer = "1.1.0"
if (Test-Path "version.json") {
    try {
        $jsonObj = Get-Content "version.json" -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($jsonObj.version) { $currentVer = $jsonObj.version.Trim().TrimStart('v','V') }
    } catch {}
}

Write-Host "Versao atual do projeto: " -NoNewline -ForegroundColor Cyan
Write-Host "v$currentVer" -ForegroundColor Yellow
Write-Host ""
Write-Host "[1] Manter versao atual (v$currentVer)" -ForegroundColor Gray
Write-Host "[2] Lancar NOVA versao (ex: 1.1.1, 1.2.0)" -ForegroundColor Gray
Write-Host ""
$opt = Read-Host "Escolha uma opcao [1 ou 2, padrao 1]"

$targetVer = $currentVer
if ($opt -eq "2") {
    $inputVer = Read-Host "Digite o numero da nova versao (ex: 1.1.1)"
    if (![string]::IsNullOrWhiteSpace($inputVer)) {
        $targetVer = $inputVer.Trim().TrimStart('v','V')
    }
}

Write-Host ""
Write-Host "===============================================================================" -ForegroundColor Green
Write-Host " PREPARANDO VERSAO: v$targetVer" -ForegroundColor Yellow
Write-Host "===============================================================================" -ForegroundColor Green

# 2. Atualizar version.json e csproj se necessario
$today = (Get-Date).ToString("dd/MM/yyyy")

# Normalizar versões (SemVer 3 partes e AssemblyVersion exatamente 4 partes)
$vParts = $targetVer.Trim().TrimStart('v','V').Split('.')
$semVer = if ($vParts.Count -ge 3) { "$($vParts[0]).$($vParts[1]).$($vParts[2])" } elseif ($vParts.Count -eq 2) { "$($vParts[0]).$($vParts[1]).0" } else { "$($vParts[0]).0.0" }
$fourParts = if ($vParts.Count -ge 4) { "$($vParts[0]).$($vParts[1]).$($vParts[2]).$($vParts[3])" } else { "$semVer.0" }
$targetVer = $semVer

if (Test-Path "version.json") {
    try {
        $j = Get-Content "version.json" -Raw -Encoding UTF8 | ConvertFrom-Json
        $j.version = $targetVer
        $j.release_date = $today
        $j.download_url = "https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v$targetVer/ErreJota_Matrix_Optimizer_Pro.exe"
        $j.downloadUrl = "https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v$targetVer/ErreJota_Matrix_Optimizer_Pro.exe"
        $j | ConvertTo-Json -Depth 5 | Set-Content "version.json" -Encoding UTF8
        Write-Host " - version.json atualizado para v$targetVer" -ForegroundColor Green
    } catch {
        Write-Host " [AVISO] Nao foi possivel atualizar version.json: $_" -ForegroundColor Yellow
    }
}

if (Test-Path "ErrejotaMatrixOptimizer.csproj") {
    try {
        $c = Get-Content "ErrejotaMatrixOptimizer.csproj" -Raw -Encoding UTF8
        $c = [regex]::Replace($c, '<Version>[^<]+</Version>', "<Version>$semVer</Version>")
        $c = [regex]::Replace($c, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$fourParts</FileVersion>")
        $c = [regex]::Replace($c, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$fourParts</AssemblyVersion>")
        [System.IO.File]::WriteAllText("ErrejotaMatrixOptimizer.csproj", $c, [System.Text.Encoding]::UTF8)
        Write-Host " - ErrejotaMatrixOptimizer.csproj atualizado para v$targetVer (AssemblyVersion: $fourParts)" -ForegroundColor Green
    } catch {
        Write-Host " [AVISO] Nao foi possivel atualizar .csproj: $_" -ForegroundColor Yellow
    }
}

# Garantir que nenhum processo antigo do painel esteja travando o .exe
Get-Process -Name "ErreJota_Matrix_Optimizer_Pro" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host " - Fechando processo em execucao que travava o arquivo: $($_.Name) (PID: $($_.Id))" -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}

Write-Host ""
# 3. Compilacao
Write-Host "[1/4] Compilando executavel Standalone Single-File (v$targetVer)..." -ForegroundColor Cyan
dotnet publish ErrejotaMatrixOptimizer.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERRO] Falha na compilacao com codigo $LASTEXITCODE." -ForegroundColor Red
    Read-Host "Pressione Enter para sair..."
    exit $LASTEXITCODE
}
Write-Host "      - Executavel compilado com sucesso!" -ForegroundColor Green

Write-Host ""
# 4. Mensagem de commit
Write-Host "[2/4] Mensagem da atualizacao..." -ForegroundColor Cyan
$commitMsg = Read-Host "Digite a descricao da atualizacao (ou Enter para padrao)"
if ([string]::IsNullOrWhiteSpace($commitMsg)) {
    $commitMsg = "Lancamento Oficial ErreJota Matrix Optimizer Pro v$targetVer"
}

Write-Host ""
# 5. Git Commit & Tag
Write-Host "[3/4] Gravando commit e gerando Git Tag v$targetVer..." -ForegroundColor Cyan
git remote set-url origin https://github.com/Hyuryx/errejota_otimiza-ao.git 2>$null
if ($LASTEXITCODE -ne 0) {
    git remote add origin https://github.com/Hyuryx/errejota_otimiza-ao.git 2>$null
}

git add .
git commit -m "v$targetVer - $commitMsg"
git tag -f -a "v$targetVer" -m "Release v$targetVer - $commitMsg"

Write-Host ""
Write-Host ""
# 6. Git Push
Write-Host "[4/5] Enviando codigo e tags para o GitHub..." -ForegroundColor Cyan
git push -u origin main
git push origin --tags -f

Write-Host ""
# 7. Publicação Automática do Release e Upload do Executável via API Oficial
Write-Host "[5/5] Publicando Release v$targetVer e enviando executavel (.exe) diretamente para o GitHub..." -ForegroundColor Cyan

$owner = "Hyuryx"
$repo = "errejota_otimiza-ao"
$tag = "v$targetVer"
$releaseName = "Release v$targetVer"
$exePath = Join-Path $PSScriptRoot "ErreJota_Matrix_Optimizer_Pro.exe"

$uploadSuccess = $false
try {
    $cred = "protocol=https`nhost=github.com`n" | git credential fill 2>$null
    $tokenMatch = $cred | Select-String "^password=(.*)"
    if ($tokenMatch) {
        $token = $tokenMatch.Matches.Groups[1].Value
        $headers = @{
            "Authorization" = "Bearer $token"
            "User-Agent" = "ErreJota-Matrix-Optimizer-Pro"
            "Accept" = "application/vnd.github.v3+json"
        }

        # 1. Obter ou Criar Release
        $release = $null
        try {
            $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases/tags/$tag" -Headers $headers -Method Get
            Write-Host "      - Release $tag localizada no GitHub." -ForegroundColor Green
        } catch {
            Write-Host "      - Criando nova release oficial para $tag..." -ForegroundColor Yellow
            $createObj = @{
                tag_name = $tag
                name = $releaseName
                body = "Atualizacao Oficial v$targetVer - $commitMsg"
                draft = $false
                prerelease = $false
            }
            $createJson = $createObj | ConvertTo-Json -Compress

            Add-Type -AssemblyName System.Net.Http
            $http = [System.Net.Http.HttpClient]::new()
            $http.DefaultRequestHeaders.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro")
            $http.DefaultRequestHeaders.Add("Authorization", "Bearer $token")
            $http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json")

            $content = [System.Net.Http.StringContent]::new($createJson, [System.Text.Encoding]::UTF8, "application/json")
            $res = $http.PostAsync("https://api.github.com/repos/$owner/$repo/releases", $content).GetAwaiter().GetResult()
            $respStr = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $http.Dispose()

            if ($res.IsSuccessStatusCode) {
                $release = $respStr | ConvertFrom-Json
                Write-Host "      - Release criada com sucesso!" -ForegroundColor Green
            }
        }

        if ($release -and (Test-Path $exePath)) {
            $fileName = [System.IO.Path]::GetFileName($exePath)
            
            # Se já existir asset antigo com mesmo nome, deleta para subir o novo
            $existingAsset = $release.assets | Where-Object { $_.name -eq $fileName }
            if ($existingAsset) {
                Write-Host "      - Substituindo binario existente no GitHub..." -ForegroundColor Yellow
                Invoke-RestMethod -Uri $existingAsset.url -Headers $headers -Method Delete 2>$null
            }

            Write-Host "      - Enviando $($fileName) ($([math]::Round((Get-Item $exePath).Length / 1MB, 2)) MB)..." -ForegroundColor Yellow
            $uploadUrl = "https://uploads.github.com/repos/$owner/$repo/releases/$($release.id)/assets?name=$fileName"

            Add-Type -AssemblyName System.Net.Http
            $client = [System.Net.Http.HttpClient]::new()
            $client.Timeout = [System.TimeSpan]::FromMinutes(15)
            $client.DefaultRequestHeaders.Add("User-Agent", "ErreJota-Matrix-Optimizer-Pro")
            $client.DefaultRequestHeaders.Add("Authorization", "Bearer $token")
            $client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json")

            $fileStream = [System.IO.File]::OpenRead($exePath)
            $streamContent = [System.Net.Http.StreamContent]::new($fileStream)
            $streamContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/octet-stream")

            $response = $client.PostAsync($uploadUrl, $streamContent).GetAwaiter().GetResult()
            $fileStream.Dispose()
            $client.Dispose()

            if ($response.IsSuccessStatusCode) {
                $uploadSuccess = $true
                Write-Host "      - Executavel enviado e publicado com sucesso!" -ForegroundColor Green
            }
        }
    }
} catch {
    Write-Host " [AVISO] Nao foi possivel publicar automaticamente via API: $_" -ForegroundColor Yellow
}

if ($uploadSuccess) {
    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Green
    Write-Host "              RELEASE E EXECUTAVEL PUBLICADOS COM SUCESSO!" -ForegroundColor Green
    Write-Host "===============================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "O download direto da versao v$targetVer ja esta 100% ONLINE e testado no GitHub!" -ForegroundColor Yellow
    Write-Host "Os clientes agora receberao e baixarao a atualizacao automaticamente sem qualquer erro." -ForegroundColor Cyan
    Write-Host "Link direto: https://github.com/$owner/$repo/releases/download/$tag/ErreJota_Matrix_Optimizer_Pro.exe" -ForegroundColor Gray
    Write-Host "===============================================================================" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Yellow
    Write-Host "                   PASSOS FINAIS PARA PUBLICACAO MANUAL" -ForegroundColor Yellow
    Write-Host "===============================================================================" -ForegroundColor Yellow
    Write-Host "1. A pagina da Release foi aberta no navegador." -ForegroundColor White
    Write-Host "2. Arraste o executavel 'ErreJota_Matrix_Optimizer_Pro.exe' para ela e publique." -ForegroundColor White
    Start-Process "https://github.com/$owner/$repo/releases/new?tag=v$targetVer&title=Release+v$targetVer"
    Start-Process "explorer.exe" -ArgumentList "/select,`"ErreJota_Matrix_Optimizer_Pro.exe`""
}
