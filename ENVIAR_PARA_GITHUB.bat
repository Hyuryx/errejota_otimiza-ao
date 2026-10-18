@echo off
chcp 65001 > NUL
title ERREJOTA MATRIX - PUBLICAR ATUALIZAÇÃO NO GITHUB
color 0A

echo ===============================================================================
echo       ERREJOTA MATRIX OPTIMIZER PRO - PUBLICAR ATUALIZACAO NO GITHUB
echo ===============================================================================
echo.

:: 1. Compila o projeto em modo Release Standalone
echo [1/4] Compilando versao final standalone (Single-File)...
dotnet publish -c Release
if %errorlevel% neq 0 (
    echo.
    echo [ERRO] Falha na compilacao. Verifique se o aplicativo esta fechado e tente novamente.
    pause
    exit /b %errorlevel%
)
echo      - Executavel compilado com sucesso!
echo.

:: 2. Inicializar Git se necessario
echo [2/4] Verificando repositorio Git...
if not exist ".git" (
    echo      - Inicializando repositorio local Git...
    git init
    git branch -M main
    git remote add origin https://github.com/Hyuryx/errejota_otimiza-ao.git
) else (
    git remote set-url origin https://github.com/Hyuryx/errejota_otimiza-ao.git
)
echo.

:: 3. Solicitar mensagem de atualizacao
echo [3/4] Preparando commit de atualizacao...
set /p COMMIT_MSG="Digite a descricao da atualizacao (ou Enter para padrao): "
if "%COMMIT_MSG%"=="" set COMMIT_MSG="Atualizacao ErreJota Matrix Optimizer Pro v1.0.0"

git add .
git commit -m "%COMMIT_MSG%"
echo.

:: 4. Enviar para o GitHub
echo [4/4] Enviando arquivos para o GitHub...
git push -u origin main
if %errorlevel% neq 0 (
    echo.
    echo [AVISO] Se o Git pediu login ou se o repositorio remoto ja tem arquivos,
    echo execute uma sincronizacao ou autorize o Git Credential Manager.
) else (
    echo.
    echo ===============================================================================
    echo      ATUALIZACAO PUBLICADA COM SUCESSO NO GITHUB!
    echo ===============================================================================
    echo.
    echo Para disponibilizar o executavel (.exe) de 142MB para download automatico:
    echo 1. Acesse: https://github.com/Hyuryx/errejota_otimiza-ao/releases/new
    echo 2. Defina a Tag com a versao (ex: v1.0.0)
    echo 3. Arraste o arquivo ErreJota_Matrix_Optimizer_Pro.exe para a area de anexos
    echo 4. Clique em 'Publish release'!
    echo.
)

pause
