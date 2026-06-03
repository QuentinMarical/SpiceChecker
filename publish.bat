@echo off
setlocal

echo ============================================
echo   SPICE CHECKER — Packaging .NET 10 (win-x64)
echo ============================================
echo.

REM Trouver le fichier projet
if exist "SpiceChecker.csproj" (
    set PROJECT=SpiceChecker.csproj
) else if exist "*.sln" (
    for %%f in (*.sln) do set PROJECT=%%f
) else (
    echo ERREUR : Aucun fichier .csproj ou .sln trouve dans le repertoire courant.
    echo Lancez ce script depuis le dossier du projet.
    exit /b 1
)

echo Projet detecte : %PROJECT%
echo.

echo [1/4] Nettoyage de la sortie precedente...
if exist "publish_output" rmdir /s /q "publish_output"

echo [2/4] Restauration des packages...
dotnet restore %PROJECT%
if errorlevel 1 (
    echo ERREUR : dotnet restore a echoue.
    exit /b 1
)

echo [3/4] Build du projet...
dotnet build %PROJECT% -c Release --no-restore
if errorlevel 1 (
    echo ERREUR : dotnet build a echoue.
    exit /b 1
)

echo [4/4] Publication en single-file self-contained...
dotnet publish %PROJECT% -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:RuntimeIdentifier=win-x64 ^
    -o "publish_output"
if errorlevel 1 (
    echo ERREUR : dotnet publish a echoue.
    exit /b 1
)

echo.
echo ============================================
echo   Packaging termine avec succes !
echo   Executable : publish_output\SpiceChecker.exe
echo ============================================
pause