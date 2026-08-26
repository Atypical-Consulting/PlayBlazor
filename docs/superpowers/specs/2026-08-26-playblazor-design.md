# PlayBlazor — Design

**Date** : 2026-08-26
**Statut** : validé (brainstorming complet avec Philippe)
**Incubation** : dans le fork MudBlazor, extraction en repo dédié une fois stable

## 1. Vision

Généraliser et remplacer les sections « Visual Playground » écrites à la main de
MudBlazor.Docs (ex. `SelectPlaygroundExample.razor` : 75 lignes de switches câblés
manuellement) par un outil standalone : **un playground de composants auto-généré
pour n'importe quelle bibliothèque Blazor**.

On pointe une assembly de composants ; l'outil découvre les composants et leurs
`[Parameter]`, génère des panneaux de contrôles typés, rend le composant en direct,
génère le snippet Razor correspondant, logge les événements et encode l'état dans
l'URL.

**Différenciation vs BlazingStory** (clone Storybook pour Blazor existant) :
BlazingStory exige une « story » `.stories.razor` écrite à la main par composant.
PlayBlazor n'exige **aucun fichier** : tout est généré par découverte.

## 2. Décisions structurantes (validées)

| Décision | Choix |
|---|---|
| Portée | REPL + panneaux auto-générés combinés, mais **phasé** |
| Distribution | Package NuGet pour auteurs de libs (modèle Storybook, embarqué dans leur site de docs) |
| Phasage code | v1 : contrôles → code généré (copiable). v2 : édition code → parsing → contrôles. v3 : vrai REPL Roslyn. Chaque phase livre un outil utilisable |
| Stack UI du shell | **Zéro dépendance UI** : HTML/CSS custom avec CSS scopé Blazor ; ~20 contrôles simples écrits à la main |
| Localisation | Incubation dans ce fork (boucle de feedback courte contre les vrais composants MudBlazor), extraction ensuite |
| Découverte | **Réflexion runtime** derrière `IComponentCatalogProvider` (un source generator pourra s'y substituer plus tard sans toucher au reste) |
| Nom de travail | **PlayBlazor** |

**Features v1 validées** : types riches (couleurs, icônes, plages numériques, slots),
code généré + permaliens, log d'événements, environnement de rendu
(thème/RTL/viewport/fond).

## 3. Architecture & projets

Trois nouveaux projets sous `src/`, **sans toucher à MudBlazor.Docs** :

- **`PlayBlazor`** — Razor Class Library, zéro dépendance UI, CSS scopé.
  Namespaces internes : `Model`, `Discovery`, `Rendering`, `CodeGen`, `Shell`.
  Un seul package en v1 (pas de split Core/UI prématuré) ; les namespaces
  rendent un split futur et l'extraction triviaux.
- **`PlayBlazor.DemoHost`** — site Blazor WASM standalone référençant MudBlazor,
  banc d'essai d'incubation. Publie en **trimmed** dès la phase 1 pour détecter
  tôt les problèmes d'élagage.
- **`PlayBlazor.UnitTests`** — bUnit + NUnit (conventions du repo).

### API publique (à garder minuscule)

```razor
@* Playground d'un composant précis, dans une page de docs *@
<PlaygroundView Component="typeof(MudSelect<string>)" />

@* Explorateur complet : arborescence de tous les composants découverts *@
<PlaygroundExplorer Assemblies="@(new[] { typeof(MudButton).Assembly })" />
```

```csharp
builder.Services.AddPlayBlazor(options =>
{
    // exclusions, presets de slots, mappers custom, ThemeWrapper, chemin XML docs…
});
```

## 4. Cœur fonctionnel

### 4.1 Modèle de descripteurs

Contrat de données central, indépendant du mécanisme de découverte :

- **`ComponentDescriptor`** : type CLR, nom d'affichage, catégorie (déduite du
  namespace), résumé XML doc, liste de `ParameterDescriptor`.
- **`ParameterDescriptor`** : nom, type, valeur par défaut, résumé, `ControlKind`
  (Bool, Enum, Text, Number, Color, Icon, Slot, Event, Unsupported).

### 4.2 Découverte — `ReflectionCatalogProvider` (implémente `IComponentCatalogProvider`)

- Scan des assemblies → `ComponentBase` publics non abstraits ; propriétés
  publiques `[Parameter]` → descripteurs.
- **Défauts** : instanciation unique par composant (`Activator.CreateInstance`
  sous try/catch) pour lire les valeurs initiales. Échec → défauts inconnus,
  composant quand même listé (avec badge d'avertissement).
- **Génériques** : `PlaygroundView` reçoit un type fermé fourni par l'hôte ;
  l'explorateur tente `T = string` puis `int`, sinon saute le composant.
- **XML docs** : parsing léger des `<summary>` depuis le fichier `.xml`
  (chemin configurable) → tooltips des contrôles.
- **Trimming** : annotations `[DynamicallyAccessedMembers]` sur les points
  d'entrée ; guide de rooting documenté.

### 4.3 Mapping type → contrôle — registre extensible `IControlMapper`

Clé de la généralisation. Défauts intégrés :

| Type | Contrôle |
|---|---|
| `bool` | switch |
| enum | select ou button-group |
| `string` | textbox |
| numérique | champ + slider si plage déclarée (attribut `[Range]` ou config hôte) |
| nullable | contrôle sous-jacent + toggle « unset » |

L'hôte enregistre ses mappers « riches » (MudBlazor : `Color` → palette, icônes →
picker alimenté par un provider d'icônes dans les options). PlayBlazor n'a
**aucune connaissance en dur** d'un type MudBlazor.

Paramètres non pilotables (objets complexes, `CascadingParameter`…) → groupe
replié « non contrôlés », ignorés au rendu.

### 4.4 Slots `RenderFragment`

Le point dur de la généralisation (un `MudSelect` vide est inutile) :

- Fallback universel : texte d'exemple éditable.
- **Presets** fournis par l'hôte :
  `options.For<MudSelect<string>>().Slot(c => c.ChildContent, Presets.UsStates)`.

### 4.5 Rendu

- `DynamicComponent` + dictionnaire de paramètres construit depuis l'état.
- `ErrorBoundary` englobant : un composant qui jette affiche l'exception + bouton
  reset, sans tuer le playground.
- **Événements** : `EventCallback` interceptés via `EventCallback.Factory.Create`
  → panneau event log (heure, nom, payload best-effort, cap 100 entrées, bouton
  clear). Si le composant expose `Value`/`ValueChanged`, branchement pour que
  l'interaction directe se reflète dans les contrôles.
- **Environnement** : conteneur englobant — fond surface/damier, largeur viewport
  contrainte, `dir="rtl"`, toggle clair/sombre. Le thème réel est délégué à
  l'hôte (`options.ThemeWrapper`, ex. `MudThemeProvider`) ; PlayBlazor fournit le
  toggle et une cascading value `PlaygroundEnvironment`.

### 4.6 Flux de données (strictement unidirectionnel)

```
contrôles → PlaygroundState → { rendu, codegen, permalien }
composant → event log
```

`PlaygroundState` ne contient que les paramètres **modifiés** (le reste = défaut) ;
sérialisable.

### 4.7 CodeGen

Snippet Razor généré depuis l'état : n'émet que les paramètres ≠ défaut,
formatage idiomatique (bool en attribut nu `Dense`, enums `Color.Primary`,
strings quotées, slots si preset). Affiché sous le rendu, mis à jour à chaque
changement, bouton copier.

### 4.8 Permaliens

État JSON compact → base64url dans l'URL (`?p=…`), restauré au chargement.
Seuls les primitifs sérialisables participent ; le reste est ignoré avec un
avertissement discret.

## 5. Robustesse

Règle : **aucune défaillance individuelle ne casse le playground.**

- Découverte best-effort : composant en échec = listé avec badge, jamais omis
  silencieusement, jamais fatal.
- Permalien corrompu/périmé (paramètre renommé) → ignoré proprement, état par
  défaut.
- DemoHost publié trimmed dès la phase 1.

## 6. Tests

- **Unitaires sur composants fixtures synthétiques** (une fixture par cas :
  chaque type de paramètre, générique, sans constructeur public, qui jette au
  rendu…) : descripteurs, mapping `ControlKind`, capture des défauts, codegen
  (comparaison à snippets attendus), round-trip permalien.
- **bUnit** : `PlaygroundView` rend une fixture ; modifier un contrôle met à jour
  rendu + code ; event log capte un `EventCallback` ; `ErrorBoundary` isole un
  composant qui jette.
- **Test de généralité** : scan de l'assembly MudBlazor complète → zéro
  exception, comptage des composants découverts. Garde-fou anti-régression du
  positionnement « généralisé ».

## 7. Jalons v1 (chaque jalon laisse un outil qui tourne)

1. **Fondations** : modèle de descripteurs + `ReflectionCatalogProvider` + tests. Pas d'UI.
2. **Premier rendu** : contrôles de base (bool/enum/string/number), `PlaygroundView`,
   DemoHost affichant `MudButton` piloté par panneaux auto-générés. *← premier moment démontrable*
3. **CodeGen** : snippet Razor live + copier.
4. **Slots & événements** : presets de `RenderFragment`, event log.
5. **Environnement & permaliens** : thème/RTL/viewport/fond, état dans l'URL.
6. **Explorer** : arborescence multi-composants, polissage, README.

## 8. Hors scope v1 (ordre prévu)

- **v2** : édition du code avec parsing → mise à jour des contrôles (sans
  compilation arbitraire).
- **v3** : vrai REPL Roslyn in-browser.
- **Extraction** : repo `PlayBlazor` dédié une fois la v1 stable ; le modèle
  hébergé multi-libs (type CodeSandbox) reste hors scope.
