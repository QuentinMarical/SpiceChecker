# Implémentation Mica / Acrylic pour SpiceChecker

## Vue d'ensemble

Ce document décrit l'implémentation complète de la transparence Mica/Acrylic (DWM) dans SpiceChecker, permettant d'avoir des effets de backdrop modernes sur Windows 10/11 tout en gardant l'application portable (single-file win-x64).

## Architecture

### 1. Services/DwmHelper.cs (déjà existant)

Classe statique gérant les appels P/Invoke vers `dwmapi.dll` pour :
- `SetDarkTitleBar` : active/désactive le dark mode sur la title bar native
- `ApplyBestEffect` : choisit automatiquement le meilleur effet selon la version Windows :
  - Windows 11 22H2+ : `DWMWA_SYSTEMBACKDROP_TYPE` (Mica/Acrylic/Tabbed)
  - Windows 11 21H2 : `DWMWA_MICA_EFFECT` (fallback)
  - Windows 10 : `SetWindowCompositionAttribute` (Acrylic legacy via blur)
- `DisableBackdrop` : désactive tous les effets DWM

### 2. Models/AppTheme.cs — Modifications

#### Nouvelles propriétés
```csharp
public bool HasNativeTitleBar => Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark;
public bool RequiresTransparentControls => Backdrop != BackdropEffect.None;
```

Ces propriétés permettent de :
- Savoir si le thème nécessite la title bar native Windows (Fluent11)
- Savoir si les contrôles doivent être transparents pour laisser passer le backdrop

### 3. Services/ThemeApplier.cs — Modifications majeures

#### Transparence des panels

```csharp
private static void MakeTransparent(Panel panel)
{
	panel.BackColor = Color.Transparent;
	var doubleBufferProperty = typeof(Control).GetProperty("DoubleBuffered",
		System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
	doubleBufferProperty?.SetValue(panel, true, null);
}
```

Active `DoubleBuffered` via réflexion pour éviter le flickering sur les panels transparents.

#### Application du thème

Dans `Apply()` :
1. **BackColor de la Form** : ajusté selon le backdrop
   - Avec backdrop : couleur semi-transparente compatible Mica (gris clair/foncé)
   - Sans backdrop : couleur opaque du thème

2. **Panels (toolbar, filter)** :
   - Avec backdrop → `Color.Transparent` + `DoubleBuffered`
   - Sans backdrop → couleur opaque du thème

3. **ToolStrip** :
   - Avec backdrop → `TransparentToolStripRenderer` (nouveau)
   - Sans backdrop → `ThemedToolStripRenderer` (existant)

4. **Labels et CheckBoxes** :
   - Avec backdrop → `BackColor = Color.Transparent`
   - Sans backdrop → couleur normale

#### TransparentToolStripRenderer (nouveau)

```csharp
internal class TransparentToolStripRenderer : ToolStripProfessionalRenderer
{
	protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
	{
		// Ne rien dessiner = transparent
	}

	protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
	{
		if (item.Selected || item.Pressed)
		{
			// Overlay semi-transparent pour hover
			using var brush = new SolidBrush(Color.FromArgb(40, _theme.ButtonHover));
			e.Graphics.FillRectangle(brush, new Rectangle(0, 0, item.Width, item.Height));
		}
	}
}
```

Renderer personnalisé pour ToolStrip qui laisse passer la transparence.

### 4. Forms/MainForm.cs — Modifications

#### ApplyCurrentTheme()

```csharp
private void ApplyCurrentTheme()
{
	EnsureCustomTitleBar();
	_titleBar.ApplyTheme(_currentTheme);
	ThemeApplier.Apply(this, _currentTheme, _grid, _toolbarPanel, _filterPanelHost, _toolbar);

	Padding = _currentTheme.HasOuterBorder3D
		? new Padding(2)
		: new Padding(1);

	EnsureTopDockOrder();
	ApplyFluentEffect();  // ← Appel ajouté
	Invalidate();
}
```

#### ApplyFluentEffect() (nouvelle méthode)

```csharp
private void ApplyFluentEffect()
{
	if (!IsHandleCreated) return;

	bool isFluent = _currentTheme.Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark;

	if (isFluent)
	{
		// Title bar native Windows
		FormBorderStyle = FormBorderStyle.Sizable;
		if (_titleBar != null) _titleBar.Visible = false;

		// Dark mode + Mica
		DwmHelper.SetDarkTitleBar(Handle, _currentTheme.Id == ThemeId.Fluent11Dark);
		DwmHelper.ApplyBestEffect(Handle, BackdropEffect.Mica, _currentTheme.BackdropFallbackTint);
	}
	else
	{
		// CustomTitleBar pour les autres thèmes
		FormBorderStyle = FormBorderStyle.None;
		if (_titleBar != null) _titleBar.Visible = true;

		// Backdrop pour Aero7 / ModernDark
		if (_currentTheme.Backdrop != BackdropEffect.None)
		{
			DwmHelper.SetDarkTitleBar(Handle, _currentTheme.IsDark);
			DwmHelper.ApplyBestEffect(Handle, _currentTheme.Backdrop, _currentTheme.BackdropFallbackTint);
		}
		else
		{
			DwmHelper.DisableBackdrop(Handle);
		}
	}
}
```

Gère :
- **Fluent11** : title bar native + Mica
- **Autres thèmes avec backdrop** (Aero7, ModernDark) : CustomTitleBar + Acrylic
- **Thèmes sans backdrop** : CustomTitleBar + aucun effet DWM

#### OnHandleCreated()

```csharp
protected override void OnHandleCreated(EventArgs e)
{
	base.OnHandleCreated(e);
	ApplyFluentEffect();  // ← Modifié pour appeler ApplyFluentEffect
}
```

Assure que l'effet est appliqué dès la création du handle de fenêtre.

### 5. Forms/CustomTitleBar.cs — Optimisation

```csharp
protected override void OnPaint(PaintEventArgs e)
{
	base.OnPaint(e);

	// Skip rendering if the theme uses native title bar
	if (_theme.HasNativeTitleBar) return;

	// ... reste du code de rendu
}
```

Évite le rendu inutile pour les thèmes Fluent11 qui utilisent la title bar native.

### 6. Services/ThemeCatalog.cs — Configuration des thèmes

Les thèmes suivants ont `Backdrop` configuré :

#### Aero7
```csharp
Backdrop = BackdropEffect.Acrylic,
BackdropFallbackTint = Color.FromArgb(160, 0, 80, 160)  // Bleu glacé semi-transparent
```

#### ModernDark
```csharp
Backdrop = BackdropEffect.Acrylic,
BackdropFallbackTint = Color.FromArgb(200, 20, 20, 20)  // Noir semi-transparent
```

#### Fluent11Light
```csharp
Backdrop = BackdropEffect.Mica,
BackdropFallbackTint = Color.FromArgb(180, 240, 240, 240)  // Gris clair
```

#### Fluent11Dark
```csharp
Backdrop = BackdropEffect.Mica,
BackdropFallbackTint = Color.FromArgb(200, 32, 32, 32)  // Gris foncé
```

## Comportement par thème

| Thème | Title Bar | FormBorderStyle | Backdrop | Contrôles transparents |
|-------|-----------|-----------------|----------|------------------------|
| **Legacy95** | CustomTitleBar | None | ❌ | ❌ |
| **LunaXP** | CustomTitleBar | None | ❌ | ❌ |
| **Aero7** | CustomTitleBar | None | ✅ Acrylic | ✅ |
| **ModernLight** | CustomTitleBar | None | ❌ | ❌ |
| **ModernDark** | CustomTitleBar | None | ✅ Acrylic | ✅ |
| **Fluent11Light** | Native Windows | Sizable | ✅ Mica | ✅ |
| **Fluent11Dark** | Native Windows | Sizable | ✅ Mica | ✅ |

## Compatibilité Windows

### Windows 11 22H2+ (build 22621+)
- **Mica** : `DWMWA_SYSTEMBACKDROP_TYPE = 2`
- **Acrylic** : `DWMWA_SYSTEMBACKDROP_TYPE = 3`
- **Tabbed** : `DWMWA_SYSTEMBACKDROP_TYPE = 4`

### Windows 11 21H2 (build 22000-22620)
- **Mica** : `DWMWA_MICA_EFFECT = 1` (fallback)
- **Acrylic** : `SetWindowCompositionAttribute` (legacy)

### Windows 10 1903+
- **Acrylic** : `SetWindowCompositionAttribute` avec `ACCENT_ENABLE_ACRYLICBLURBEHIND`

### Windows 10 < 1903 ou VM sans DWM
- Aucun effet (dégradation gracieuse)

## Contraintes respectées

✅ **Portable single-file** : utilise uniquement `dwmapi.dll` (DLL système Windows), jamais embarqué  
✅ **Pas de modification du pipeline** : filtres → évaluation → rendu intact  
✅ **Pas de modification IRule** : signatures inchangées  
✅ **Nullable activé** : tous les déréférencements null vérifiés  
✅ **Thèmes legacy préservés** : Legacy95, LunaXP conservent CustomTitleBar + FormBorderStyle.None  
✅ **publishable via publish.bat** : aucune dépendance externe ajoutée

## Testing

### Tests visuels recommandés

1. **Fluent11Light** sur Windows 11 :
   - Title bar native Windows avec couleurs claires
   - Mica visible à travers les panels toolbar/filter
   - Labels/CheckBoxes transparents

2. **Fluent11Dark** sur Windows 11 :
   - Title bar native Windows en dark mode
   - Mica sombre visible à travers l'interface

3. **Aero7** sur Windows 11 ou 10 :
   - CustomTitleBar avec effet vitre
   - Acrylic derrière la fenêtre
   - Contrôles semi-transparents

4. **ModernDark** sur Windows 11 ou 10 :
   - CustomTitleBar plate
   - Acrylic sombre derrière la fenêtre

5. **Legacy95 / LunaXP** :
   - CustomTitleBar opaque (pas de transparence)
   - Comportement identique à avant

### Tests de compatibilité

- **Windows 11 22H2+** : Mica natif
- **Windows 11 21H2** : Mica legacy
- **Windows 10 20H1+** : Acrylic legacy
- **Windows 10 < 20H1** : dégradation gracieuse (opaque)
- **VM sans DWM** : dégradation gracieuse (opaque)

## Vanara.PInvoke.DwmApi

Le package `Vanara.PInvoke.DwmApi 5.0.5` reste dans le projet car il est utilisé dans `DwmHelper.SetTitleBarDarkMode` comme fallback. Si vous souhaitez le retirer, remplacez tous les appels à `DwmApi.DwmSetWindowAttribute` par les P/Invoke directs déjà présents dans `DwmHelper` (`SetDarkTitleBar`).

## Évolutions possibles

1. **Choix utilisateur Mica/Acrylic** : ajouter un paramètre dans `SettingsService` pour choisir entre Mica, Acrylic ou Tabbed
2. **Transparence DataGridView** : utiliser un `DataGridView` custom avec `DoubleBuffered` et cellules semi-transparentes
3. **Acrylic Tint personnalisable** : permettre à l'utilisateur de choisir la couleur de teinte
4. **Animation de transition** : animer le changement de thème avec fade in/out

## Résumé des fichiers modifiés

| Fichier | Modifications |
|---------|---------------|
| **Services/ThemeApplier.cs** | Ajout transparence panels/labels, `TransparentToolStripRenderer` |
| **Models/AppTheme.cs** | Ajout `HasNativeTitleBar`, `RequiresTransparentControls` |
| **Forms/MainForm.cs** | Ajout `ApplyFluentEffect()`, modification `OnHandleCreated()` |
| **Forms/CustomTitleBar.cs** | Optimisation `OnPaint()` pour skip si native title bar |
| **Services/DwmHelper.cs** | (Déjà existant, pas modifié) |
| **Services/ThemeCatalog.cs** | (Configuration existante des backdrops, pas modifié) |

---

**Date** : 2025  
**Version** : .NET 10  
**Auteur** : Implémentation basée sur les spécifications DWM Windows 11/10
