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
        $c = [regex]::Replace($c, '<Version>[^<]+</Version>', "<Version>$targetVer</Version>")
        $c = [regex]::Replace($c, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$targetVer.0</FileVersion>")
        $c = [regex]::Replace($c, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$targetVer.0</AssemblyVersion>")
        [System.IO.File]::WriteAllText("ErrejotaMatrixOptimizer.csproj", $c, [System.Text.Encoding]::UTF8)
        Write-Host " - ErrejotaMatrixOptimizer.csproj atualizado para v$targetVer" -ForegroundColor Green
    } catch {
        Write-Host " [AVISO] Nao foi possivel atualizar .csproj: $_" -ForegroundColor Yellow
    }
}

Write-Host ""
# 3. Compilacao
Write-Host "[1/4] Compilando executavel Standalone Single-File (v$targetVer)..." -ForegroundColor Cyan
dotnet publish ErrejotaMatrixOptimizer.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERRO] Falha na compilacao. Certifique-se de que o ErreJota_Matrix_Optimizer_Pro.exe nao esta em execucao." -ForegroundColor Red
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
# 6. Git Push
Write-Host "[4/4] Enviando codigo e tags para o GitHub..." -ForegroundColor Cyan
git push -u origin main
git push origin --tags -f

Write-Host ""
Write-Host "===============================================================================" -ForegroundColor Green
Write-Host "                    ATUALIZACAO ENVIADA COM SUCESSO!" -ForegroundColor Green
Write-Host "===============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "A tag v$targetVer ja esta criada no GitHub!" -ForegroundColor Yellow
Write-Host ""
Write-Host "PASSO FINAL (1 CLIQUE):" -ForegroundColor White
Write-Host "1. A pagina de criacao de Release no GitHub foi aberta no seu navegador." -ForegroundColor White
Write-Host "2. A pasta com o arquivo 'ErreJota_Matrix_Optimizer_Pro.exe' selecionado foi aberta." -ForegroundColor White
Write-Host "3. Basta ARRASTAR o arquivo .exe para a pagina do GitHub e clicar em 'Publish release'!" -ForegroundColor Green
Write-Host ""
Write-Host "Assim que publicado la, o aplicativo dos clientes atualizara sozinho ao clicar em 'Verificar Atualizacoes'!" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Green
Write-Host ""

Start-Process "https://github.com/Hyuryx/errejota_otimiza-ao/releases/new?tag=v$targetVer&title=Release+v$targetVer"
Start-Process "explorer.exe" -ArgumentList "/select,`"ErreJota_Matrix_Optimizer_Pro.exe`""
