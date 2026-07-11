# SpiceChecker

SpiceChecker est une application Windows Forms destinée au contrôle et à l’analyse de données métier, avec une architecture .NET découpée en plusieurs projets.

## Architecture active

La structure actuellement utilisée par la solution est la suivante :

- `SpiceChecker.Domain` : entités et logique métier centrale (règles SPICE)
- `SpiceChecker.Application` : services applicatifs et cas d’usage
- `SpiceChecker.Infrastructure` : implémentations techniques (import Excel, exports, paramètres)
- `SpiceChecker.WinForms` : interface utilisateur Windows Forms
- `SpiceChecker.Domain.Tests` : tests des règles métier et de la validation
- `SpiceChecker.Infrastructure.Tests` : tests de l’import des exports SPICE (alm_hardware)

La solution XML `SpiceChecker.slnx` référence uniquement cette architecture multi-projets.

## Format d’import attendu

L’application importe les exports SPICE / ServiceNow de la table `alm_hardware` avec les colonnes :
`Etiquette`, `État`, `Sous-état`, `Entrepôt`, `Catégorie de modèle`, `Modèle`, `Date dernier sous état`.

- La RAM et le fabricant sont extraits du nom de modèle (ex. `LENOVO THINKPAD_L14 G4 R5 16 256Go` → Lenovo, 16 Go).
- Les colonnes facultatives `Commentaire` et `Date de renouvellement de matériel` sont exploitées si elles sont
  présentes dans l’export : elles rendent automatiques le contrôle du commentaire de panne et la règle
  « 2027 → Revalorisation / 2028 → Réparation » des L13 G2 / L14.

## Règles métier (consignes Enedis / Econocom consolidées, mai 2026)

1. **Lenovo 16/32 Go (prioritaire)** : matériel stratégique (pénurie HP), jamais en Revalorisation ni en don.
   Fonctionnel → Disponible Re-Use ; défectueux → Réparation. Les L14 G2 I7/R7 32 Go sont à garder pour TMI.
2. **L13 / L14 8 Go** : fonctionnels → Disponible Re-Use, peu importe la date de renouvellement.
   L13 G1 défectueux → Revalorisation. L13 G2 / L14 défectueux → renouvellement 2027 ou avant → Revalorisation,
   2028 ou après → Réparation. Commentaire de panne obligatoire dans les deux cas.
3. **Revalorisation automatique** (sans contrainte de date) : L390 et modèles antérieurs (L450, L460, L470,
   L480, T580…), Surface Pro défectueux (non pris en charge par le service réparation Enedis), ordinateurs
   non Lenovo/HP défectueux.
4. **Revalorisation** : uniquement du matériel défectueux, avec un commentaire indiquant la nature de la panne
   (minimum 10 caractères) ; sinon requalifier en Disponible Re-Use.
5. **Blanchiment** : « A blanchir » est une étape intermédiaire ; après blanchiment → Disponible Re-Use.
6. **Sous-états figés** : tout sous-état transitoire (Revalorisation, A blanchir, Défectueux, don, réparation)
   inchangé depuis plus de 6 mois est signalé.
7. `Disponible neuf`, `Disponible Re-Use`, `Réservé/Masterisé` et `Reprise en attente` sont des états légitimes,
   jamais signalés en anomalie.

Ces règles évoluent chaque trimestre : elles sont centralisées dans `SpiceChecker.Domain/Rules/` et couvertes
par `SpiceChecker.Domain.Tests`.

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