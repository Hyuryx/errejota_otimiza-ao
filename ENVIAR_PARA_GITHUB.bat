@echo off
chcp 65001 > NUL
setlocal enabledelayedexpansion
cd /d "%~dp0"
title ERREJOTA MATRIX - PUBLICADOR AUTOMÁTICO DE ATUALIZAÇÕES
color 0A

echo ===============================================================================
echo       ERREJOTA MATRIX OPTIMIZER PRO - PUBLICADOR AUTOMÁTICO DE ATUALIZAÇÕES
echo ===============================================================================
echo.

:: 1. Detectar versão atual do version.json
set "CURRENT_VER=1.1.0"
for /f "tokens=2 delims=:, " %%a in ('findstr /i "\"version\"" version.json 2^>nul') do (
    set "RAW_VER=%%a"
    set "RAW_VER=!RAW_VER:"=!"
    set "RAW_VER=!RAW_VER: =!"
    if not "!RAW_VER!"=="" set "CURRENT_VER=!RAW_VER!"
)

echo Versão atual detectada no projeto: v!CURRENT_VER!
echo.
echo Deseja lançar uma nova versão ou recompilar a versão atual?
echo [1] Manter versão atual (!CURRENT_VER!)
echo [2] Lançar NOVA versão personalizada (ex: 1.1.1, 1.2.0, etc.)
echo.
set /p "OPT_VER=Escolha uma opção [1 ou 2, padrão 1]: "

set "TARGET_VER=!CURRENT_VER!"
if "!OPT_VER!"=="2" (
    echo.
    set /p "TARGET_VER=Digite a nova versão (ex: 1.1.1): "
    if "!TARGET_VER!"=="" set "TARGET_VER=!CURRENT_VER!"
)

:: Remover qualquer 'v' inicial se o usuário digitou v1.1.1
if "!TARGET_VER:~0,1!"=="v" set "TARGET_VER=!TARGET_VER:~1!"
if "!TARGET_VER:~0,1!"=="V" set "TARGET_VER=!TARGET_VER:~1!"

echo.
echo ===============================================================================
echo  PREPARANDO VERSÃO ALVO: v!TARGET_VER!
echo ===============================================================================

:: 2. Atualizar automaticamente version.json e csproj se a versão for alterada
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$v = '!TARGET_VER!';" ^
    "$today = (Get-Date).ToString('dd/MM/yyyy');" ^
    "if (Test-Path 'version.json') {" ^
    "    $j = Get-Content 'version.json' -Raw | ConvertFrom-Json;" ^
    "    $j.version = $v;" ^
    "    $j.release_date = $today;" ^
    "    $j.download_url = \"https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v$v/ErreJota_Matrix_Optimizer_Pro.exe\";" ^
    "    $j.downloadUrl = \"https://github.com/Hyuryx/errejota_otimiza-ao/releases/download/v$v/ErreJota_Matrix_Optimizer_Pro.exe\";" ^
    "    $j | ConvertTo-Json -Depth 5 | Set-Content 'version.json' -Encoding utf8;" ^
    "}" ^
    "if (Test-Path 'ErrejotaMatrixOptimizer.csproj') {" ^
    "    $c = Get-Content 'ErrejotaMatrixOptimizer.csproj' -Raw;" ^
    "    $c = [regex]::Replace($c, '<Version>[^<]+</Version>', \"<Version>$v</Version>\");" ^
    "    $c = [regex]::Replace($c, '<FileVersion>[^<]+</FileVersion>', \"<FileVersion>$v.0</FileVersion>\");" ^
    "    $c = [regex]::Replace($c, '<AssemblyVersion>[^<]+</AssemblyVersion>', \"<AssemblyVersion>$v.0</AssemblyVersion>\");" ^
    "    [System.IO.File]::WriteAllText('ErrejotaMatrixOptimizer.csproj', $c, [System.Text.Encoding]::UTF8);" ^
    "}"
echo      ✔ Arquivos de versionamento sincronizados com v!TARGET_VER!
echo.

:: 3. Compilar em Release Standalone
echo [1/4] Compilando executavel Standalone Single-File (v!TARGET_VER!)...
dotnet publish ErrejotaMatrixOptimizer.csproj -c Release
if %errorlevel% neq 0 (
    echo.
    echo [ERRO] Falha na compilação. Certifique-se de que o ErreJota_Matrix_Optimizer_Pro.exe não está aberto.
    pause
    exit /b %errorlevel%
)
echo      ✔ Compilação concluída com sucesso!
echo.

:: 4. Solicitar descrição do update
echo [2/4] Preparando mensagem da atualização...
set "COMMIT_MSG="
set /p "COMMIT_MSG=Digite a descrição desta atualização (ou pressione Enter para padrão): "
if not defined COMMIT_MSG set "COMMIT_MSG=Lançamento Oficial ErreJota Matrix Optimizer Pro v!TARGET_VER!"

:: 5. Git Commit & Tag
echo.
echo [3/4] Gravando commit e gerando tag de versão v!TARGET_VER!...
if not exist ".git" (
    git init
    git branch -M main
    git remote add origin https://github.com/Hyuryx/errejota_otimiza-ao.git
) else (
    git remote set-url origin https://github.com/Hyuryx/errejota_otimiza-ao.git
)

git add .
git commit -m "v!TARGET_VER! - %COMMIT_MSG%"
git tag -f -a "v!TARGET_VER!" -m "Release v!TARGET_VER! - %COMMIT_MSG%"

:: 6. Push para o GitHub
echo.
echo [4/4] Enviando código e tags para o GitHub...
git push -u origin main
git push origin --tags -f

echo.
echo ===============================================================================
echo                    🚀 ATUALIZAÇÃO ENVIADA COM SUCESSO!
echo ===============================================================================
echo.
echo A tag v!TARGET_VER! e o version.json já estão online no GitHub!
echo.
echo PASSO FINAL PARA DISPONIBILIZAR O DOWNLOAD AUTOMÁTICO:
echo 1. Uma aba do navegador será aberta diretamente na criação da Release v!TARGET_VER!.
echo 2. Uma janela do Explorer será aberta com o executável 'ErreJota_Matrix_Optimizer_Pro.exe' selecionado.
echo 3. Basta ARRASTAR o arquivo .exe para a página do GitHub e clicar no botão 'Publish release'!
echo.
echo Assim que clicar em 'Publish release', QUALQUER usuário com o programa instalado
echo receberá a notificação e atualizará automaticamente com 1 clique!
echo ===============================================================================
echo.

:: Abrir navegador na página de nova release e abrir explorer selecionando o exe
start "" "https://github.com/Hyuryx/errejota_otimiza-ao/releases/new?tag=v!TARGET_VER!&title=Release+v!TARGET_VER!"
start "" explorer.exe /select,"ErreJota_Matrix_Optimizer_Pro.exe"

pause
