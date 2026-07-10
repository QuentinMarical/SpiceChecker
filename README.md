# SpiceChecker

SpiceChecker est une application Windows Forms destinée au contrôle et à l’analyse de données métier, avec une architecture .NET découpée en plusieurs projets.

## Architecture active

La structure actuellement utilisée par la solution est la suivante :

- `SpiceChecker.Domain` : entités et logique métier centrale
- `SpiceChecker.Application` : services applicatifs et cas d’usage
- `SpiceChecker.Infrastructure` : implémentations techniques et accès aux données
- `SpiceChecker.WinForms` : interface utilisateur Windows Forms
- `SpiceChecker.Application.Tests`
- `SpiceChecker.Domain.Tests`
- `SpiceChecker.Infrastructure.Tests`
- `SpiceChecker.WinForms.Tests`

La solution XML `SpiceChecker.slnx` référence uniquement cette architecture multi-projets.

## Point d’entrée principal

Le projet à utiliser pour lancer l’application est :

- `SpiceChecker.WinForms/SpiceChecker.WinForms.csproj`

Le point d’entrée principal se trouve dans :

- `SpiceChecker.WinForms/Program.cs`

## Éléments historiques archivés

L’ancienne structure monolithique du projet a été déplacée dans :

- `Legacy/OldRootApp/`

Cette archive contient les anciens fichiers et dossiers conservés à titre historique, notamment l’ancienne organisation racine du projet.

Ces éléments ne font pas partie de la solution moderne référencée par `SpiceChecker.slnx` et ne doivent plus être utilisés comme base de développement principale.

## Build recommandé

```powershell
dotnet restore SpiceChecker.WinForms/SpiceChecker.WinForms.csproj
dotnet build SpiceChecker.WinForms/SpiceChecker.WinForms.csproj -c Release
```

## Publication

Le script `publish.bat` publie explicitement le projet :

- `SpiceChecker.WinForms/SpiceChecker.WinForms.csproj`

## Objectif du dépôt

L’objectif est de poursuivre la migration vers une structure claire, testable et maintenable, en isolant les artefacts hérités de l’ancienne version.

## Prochaine étape de nettoyage

Les prochaines améliorations possibles concernent principalement :
- la documentation de `Legacy/OldRootApp` ;
- l’ajout d’une CI GitHub Actions ;
- l’ajout d’une description et de tags GitHub ;
- la préparation d’une première release.