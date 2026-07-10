@echo off
setlocal

echo ============================================
echo SPICE CHECKER — Publication WinForms .NET 10
echo ============================================
echo.

set PROJECT=SpiceChecker.WinForms\SpiceChecker.WinForms.csproj
set OUTPUT=publish_output

if not exist "%PROJECT%" (
    echo ERREUR : Projet introuvable : %PROJECT%
    echo Lancez ce script depuis la racine du depot.
    exit /b 1
)

echo Projet cible : %PROJECT%
echo.

echo [1/4] Nettoyage de la sortie precedente...
if exist "%OUTPUT%" rmdir /s /q "%OUTPUT%"

echo [2/4] Restauration des packages...
dotnet restore "%PROJECT%"
if errorlevel 1 (
    echo ERREUR : dotnet restore a echoue.
    exit /b 1
)

echo [3/4] Build du projet...
dotnet build "%PROJECT%" -c Release --no-restore
if errorlevel 1 (
    echo ERREUR : dotnet build a echoue.
    exit /b 1
)

echo [4/4] Publication en single-file self-contained...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:RuntimeIdentifier=win-x64 ^
    -o "%OUTPUT%"
if errorlevel 1 (
    echo ERREUR : dotnet publish a echoue.
    exit /b 1
)

echo.
echo ============================================
echo Publication terminee avec succes !
echo Executable : %OUTPUT%\SpiceChecker.WinForms.exe
echo ============================================
pause