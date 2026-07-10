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

## Éléments historiques

Le dépôt a contenu une ancienne structure monolithique à la racine.  
Les éléments suivants doivent être considérés comme historiques ou en cours de retrait :

- `Program.cs`
- `SpiceChecker.csproj`
- `Probe.cs`
- `publish.bat`
- `Controls/`
- `Forms/`
- `Models/`
- `Rules/`
- `Services/`

Ces éléments ne font pas partie de la solution moderne référencée par `SpiceChecker.slnx`.

## Build recommandé

```powershell
dotnet restore SpiceChecker.WinForms/SpiceChecker.WinForms.csproj
dotnet build SpiceChecker.WinForms/SpiceChecker.WinForms.csproj -c Release
```

## Objectif du dépôt

L’objectif est de poursuivre la migration vers une structure claire, testable et maintenable, en éliminant progressivement les artefacts hérités de l’ancienne version.