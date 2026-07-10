# Legacy

Ce dossier regroupe les éléments historiques archivés lors du nettoyage du dépôt SpiceChecker.

## OldRootApp

`OldRootApp/` contient l’ancienne structure monolithique qui était auparavant présente à la racine du dépôt :

- anciens formulaires Windows Forms
- anciens modèles métier
- anciennes règles et services
- ancien projet racine conservé à titre d’archive

Ces fichiers sont conservés pour référence ou migration ponctuelle, mais ne font plus partie de la solution active `SpiceChecker.slnx`.

Le développement courant doit se faire uniquement sur l’architecture moderne :

- `SpiceChecker.Domain`
- `SpiceChecker.Application`
- `SpiceChecker.Infrastructure`
- `SpiceChecker.WinForms`