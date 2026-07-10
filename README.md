# SpiceChecker

SpiceChecker est une application Windows Forms destinée à l’analyse et au contrôle de données d’inventaire/export, avec import Excel, règles métier et filtres d’anomalies.

## Structure du dépôt

Le dépôt contient actuellement deux générations d’architecture :

### Architecture active

La structure active du projet repose sur plusieurs projets .NET séparés :

- `SpiceChecker.Domain` : entités et logique métier centrale
- `SpiceChecker.Application` : cas d’usage et services applicatifs
- `SpiceChecker.Infrastructure` : accès aux données et implémentations techniques
- `SpiceChecker.WinForms` : interface utilisateur Windows Forms
- `SpiceChecker.*.Tests` : projets de tests associés

Le point d’entrée principal de l’application se trouve dans :

- `SpiceChecker.WinForms/Program.cs`

## Éléments legacy

Le dépôt contient encore plusieurs fichiers et dossiers historiques à la racine :

- `Program.cs`
- `SpiceChecker.csproj`
- `SpiceChecker - old (à conserver).csproj`
- `Controls/`
- `Forms/`
- `Models/`
- `Rules/`
- `Services/`
- `publish.bat`

Ces éléments correspondent à l’ancienne structure monolithique du projet. Ils sont conservés temporairement pendant la transition, mais ne constituent plus la cible recommandée pour les évolutions futures.

## Build recommandé

Utiliser le projet WinForms moderne :

```powershell
dotnet restore SpiceChecker.WinForms/SpiceChecker.WinForms.csproj
dotnet build SpiceChecker.WinForms/SpiceChecker.WinForms.csproj -c Release
```

## Publication

Le script `publish.bat` est prévu pour publier explicitement le projet `SpiceChecker.WinForms`.

## Objectif du nettoyage

À terme, le dépôt a vocation à :
- documenter clairement l’architecture active ;
- isoler ou supprimer les artefacts legacy ;
- simplifier l’ouverture du projet dans Visual Studio ;
- éviter toute ambiguïté entre ancien et nouveau point d’entrée.

## État actuel

Le dépôt est fonctionnel, mais un nettoyage progressif de la racine est encore nécessaire pour rendre la structure plus lisible.