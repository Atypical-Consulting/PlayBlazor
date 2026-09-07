# PlayBlazor — Playgrounds multi-librairies (MudBlazor · Fluent UI · DaisyBlazor)

**Date** : 2026-09-07
**Statut** : validé (brainstorming complet avec Philippe)
**Précédent** : [2026-08-26-playblazor-design.md](2026-08-26-playblazor-design.md)

## 1. Objectif

Prouver, en le faisant, que PlayBlazor est agnostique de la librairie qu'il explore :
trois vitrines curées à parité — **MudBlazor**, **Microsoft Fluent UI Blazor v5** et
**DaisyBlazor** — publiées sous un seul site GitHub Pages, avec un sélecteur de librairie.

Le constat de départ : `src/PlayBlazor` ne contient presque rien de spécifique à MudBlazor.
Tout le savoir-librairie vit déjà côté hôte dans `PlayBlazorOptions` (`ComponentFilter`,
`IconResolver`, `ThemeWrapper`, presets, scaffolds, variants, exclusions). Le travail est donc
**surtout de la configuration hôte**, plus un trou réel dans le package : les icônes.

## 2. Décisions structurantes (validées)

| Décision | Choix |
|---|---|
| Forme de la démo | **Trois apps WASM, un site Pages, un sélecteur qui est un lien.** Isolation CSS/JS totale, payload par librairie |
| Version Fluent UI | **v5 RC** (`5.0.0-rc.5-26219.1`) — pur Blazor, plus de Fluent UI Web Components |
| Source DaisyBlazor | **1.0.0**, après merge de la PR release-please `phmatray/blazor-tailwind-ui#20` |
| Ampleur des presets | **Parité complète avec MudBlazor** — ~60 composants curés par librairie |
| Extension du package | **Le sac d'options grandit** : un catalogue de valeurs par type. Pas de paquets satellites (pour l'instant) |
| Paquets publiés | **1, inchangé.** `src/PlayBlazor` garde zéro dépendance UI |

### Pourquoi trois apps et non une

Une app unique référençant les trois librairies empilerait dans un même document le preflight
Tailwind v4 (DaisyBlazor), `MudBlazor.min.css` et le reboot Fluent. Chacun réinitialise
globalement. `CLAUDE.md` note déjà qu'une feuille de style hôte a atteint le spécimen **trois
fois** avec MudBlazor seul ; à trois librairies c'est une garantie, pas un risque.

### Pourquoi pas de profils en paquets satellites

`PlayBlazor.MudBlazor` / `.FluentUI` / `.DaisyBlazor` offriraient la meilleure expérience
consommateur, mais concevraient l'abstraction depuis **un seul** point de données. Écrire les
trois configs à parité est précisément l'expérience qui révèle ce qui mérite d'être un profil.
Voir §9.

## 3. Le package : un catalogue de valeurs par type

### 3.1 Le problème, mesuré

Les trois librairies représentent une icône de trois façons incompatibles :

| Librairie | Type du paramètre | Contenu de la valeur | Comportement actuel |
|---|---|---|---|
| MudBlazor | `string` | markup SVG interne | `ControlKind.Icon`, aperçu ✅ |
| Fluent UI v5 | **`Icon` (objet)** | `.Content` ; `.ToMarkup()`, `.ToDataUri()` | `Unsupported` — **aucun contrôle** ❌ |
| DaisyBlazor | `string` | nom de ligature Material (`"ac_unit"`) | `Icon`, mais **aucun aperçu** ⚠️ |

Cause : `ReflectionCatalogProvider.cs:91-97` décide `ControlKind.Icon` par une heuristique
purement nominale — type `string` **et** nom de propriété finissant par `Icon`. Un paramètre
typé `Icon` n'atteint jamais cette heuristique ; une ligature l'atteint mais échoue au test
`markup.TrimStart().StartsWith('<')` de `IconControl.razor:5`.

`ControlKind.Icon` porte depuis l'origine la doc « *Reserved for host-registered rich mappers
(icon pickers…)* ». L'emplacement a été prévu et jamais rempli.

### 3.2 L'API

```csharp
public sealed class PlayBlazorOptions
{
    /// <summary>Named values a host offers for one parameter type, with an optional preview.</summary>
    public PlayBlazorOptions Catalogue<T>(
        IReadOnlyDictionary<string, T?> named,
        Func<T?, RenderFragment>? preview = null);

    internal bool TryGetCatalogue(Type type, out CatalogueDefinition catalogue);
}
```

### 3.3 La règle qui évite le piège

**Un catalogue remplit un `ControlKind`, il ne l'élargit jamais.**

`ControlKind.Icon` est décidé par l'union de deux règles :

1. l'heuristique actuelle (type `string` **et** nom finissant par `Icon`) — inchangée ;
2. **nouvelle** : le type du paramètre a un catalogue enregistré.

Conséquences voulues :

- `Catalogue<Icon>(…)` fait passer tout paramètre typé `Icon` en contrôle piloté — Fluent est
  débloqué par la règle 2.
- `Catalogue<string>(…)` **ne s'applique qu'aux `string` que la règle 1 avait déjà marqués
  `Icon`**. Sans cette restriction, enregistrer un catalogue sur `string` transformerait tous
  les champs texte de la librairie en sélecteur.

`ControlKind.Icon` n'est pas renommé et aucun membre d'enum n'est ajouté : sa documentation est
réécrite pour décrire enfin ce qu'il fait. Aucune rupture d'API.

### 3.4 Le contrôle

`IconControl` devient un **sélecteur cherchable** — liste filtrable de noms, vignettes quand le
catalogue fournit un `preview`. Il retombe sur le champ texte + aperçu SVG actuel quand aucun
catalogue n'est enregistré pour le type, ce qui préserve exactement le comportement d'un hôte
existant.

Les trois formes d'aperçu sont fournies par l'hôte, pas devinées par le package :

| Librairie | `preview` de l'hôte |
|---|---|
| MudBlazor | `<svg viewBox="0 0 24 24">@((MarkupString)value)</svg>` |
| Fluent UI | `@((MarkupString)icon.ToMarkup())` |
| DaisyBlazor | `<span class="material-symbols-outlined">@value</span>` |

**Effet de bord voulu** : la démo MudBlazor y gagne aussi. Coller du markup SVG brut à la main
cesse d'être la seule façon de changer une icône.

### 3.5 Ce qui ne change pas

`ControlKindResolver.LooksLikeColor` reste structurel (propriétés R/G/B + constructeur `string`).
Il ne matchera ni Fluent ni DaisyBlazor — c'est acceptable et documenté : un preset hôte reste
disponible pour ces paramètres.

## 4. La démo : quatre projets

```
demo/
  PlayBlazor.Demo.Shared/      RCL — chrome, hero, tuiles, sélecteur, demo.css. ZÉRO dépendance UI.
  PlayBlazor.Demo.MudBlazor/   l'actuel PlayBlazor.DemoHost, déplacé tel quel
  PlayBlazor.Demo.FluentUI/
  PlayBlazor.Demo.Daisy/
```

`Pages/Index.razor` est déjà générique à 90 % — il ne code en dur que les types MudBlazor et le
texte du pied de page. Il devient un composant du projet partagé :

```razor
<DemoLanding Assembly="…" Flagships="…" LibraryName="…" DocsUrl="…" />
```

**`Demo.Shared` ne référence aucune librairie UI.** C'est la contrainte load-bearing du repo qui
fuirait sinon dans la démo.

### La racine du site est statique

`https://atypical-consulting.github.io/PlayBlazor/` devient une **page HTML statique** — trois
cartes, trois liens — et non une app WASM. Le sélecteur ne coûte jamais un runtime .NET, et le
site ouvre instantanément. Chaque app garde sa propre vitrine sur son `/` et son `/explorer`.

```
/PlayBlazor/            landing statique + sélecteur
/PlayBlazor/mud/        app WASM — MudBlazor.min.css seul
/PlayBlazor/fluent/     app WASM — assets Fluent seuls
/PlayBlazor/daisy/      app WASM — build Tailwind seul
```

Le sélecteur présent dans chaque app est une bande d'en-tête de trois liens `<a>` : la librairie
courante est active, les deux autres sont des liens relatifs (`../fluent/`). Changer de
librairie recharge la page — accepté.

### Racines de trimmer

Chaque app enracine l'assembly qu'elle explore, sans quoi le trimmer décapite la réflexion
(cf. `CLAUDE.md`) :

| App | `TrimmerRootAssembly` |
|---|---|
| mud | `MudBlazor` |
| fluent | `Microsoft.FluentUI.AspNetCore.Components` |
| daisy | `DaisyBlazor.Components` |

## 5. La CI

`deploy-demo.yml` passe en matrice :

```yaml
strategy:
  matrix:
    app: [mud, fluent, daisy]
```

Chaque branche : `dotnet publish` vers `publish/<app>`, réécriture du base href en
`/PlayBlazor/<app>/`, **conservation de l'étape de vérification du base href** (elle empêche de
livrer un site dont tous les assets renvoient 404 sans que le build ne bronche), installation du
`404.html` de repli SPA.

Un job final assemble `publish/`, y dépose la landing statique et `.nojekyll`, puis fait un seul
`upload-pages-artifact`.

Le job `daisy` ajoute, avant `dotnet publish` : `setup-node`, `npm ci`, build Tailwind
(`tailwindcss -i Styles/main.css -o wwwroot/css/app.css --minify`). `PlayBlazor.Demo.Daisy`
porte donc ses propres `package.json` / `package-lock.json` — c'est la seule app du repo à
avoir une dépendance Node, et elle est confinée à ce projet.

Le câblage CSS DaisyBlazor suppose `@daisyblazor/tailwind` publié sur npm — ce que
`publish.yml` du monorepo fait déjà au tag (« *The daisyUI Tailwind preset ships to npm as
@daisyblazor/tailwind, versioned in lockstep* »). Il n'a jamais été publié à ce jour ; le merge
de la PR #20 le publie.

## 6. Les tests

**Le sweep d'abord, la config ensuite.** `[Explicit] ListUnsupportedParameterTypes` et
`RenderSweep` sont paramétrés par librairie et lancés sur Fluent et Daisy **avant** d'écrire
400 lignes de config. C'est le sweep qui dit ce qui manque, pas l'intuition.

Nouveaux tests ciblés :

- un catalogue sur un type non-`string` fait passer le paramètre en `ControlKind.Icon` ;
- un catalogue sur `string` **n'élargit pas** — un `string` ordinaire reste `ControlKind.Text` ;
- l'aperçu du catalogue est rendu quand `preview` est fourni, et le champ texte historique
  subsiste quand aucun catalogue n'est enregistré ;
- sérialisation/permalien d'une valeur issue d'un catalogue (`ParameterValueConverter`).

Le projet de tests peut référencer les trois librairies : c'est le package qui doit rester
propre, pas les tests.

## 7. Les risques

| Risque | Traitement |
|---|---|
| **Fluent v5 est en RC** — ~400 lignes de config exposées à une API qui bougera | épingler la RC exacte ; accepter la churn, c'est le prix du choix « pur Blazor » |
| **La safelist DaisyBlazor sera prise en défaut** — le playground compose des classes à l'exécution (`btn-accent`, `badge-xl`) que Tailwind n'a jamais vues | attendu : c'est le test de complétude le plus dur jamais fait sur ce kit, et la valeur revient à DaisyBlazor |
| **Poids du site ×3** | chaque app se télécharge seule ; la landing est statique |
| **`Demo.Shared` pourrait dériver vers une dépendance UI** | discipline de revue ; la contrainte est déjà écrite dans `CLAUDE.md` |
| **Daisy bloquée sur le merge de #20** | les jalons 1 à 3 n'en dépendent pas |

## 8. Jalons

1. **Hooks du package** — catalogue de valeurs, `IconControl` en sélecteur, tests. Débloque tout
   le reste.
2. **Restructuration `demo/`** en quatre projets, MudBlazor déplacé tel quel, CI en matrice,
   landing statique. Le site tourne à trois apps dont deux vides.
3. **Fluent UI** — sweep, puis `PlaygroundConfig` à parité.
4. **DaisyBlazor** — merge de #20, montée en 1.0.0, sweep, puis `PlaygroundConfig` à parité.

Les jalons 1 et 2 ne changent **aucun** comportement visible de la démo MudBlazor actuelle :
c'est le filet de sécurité de la restructuration.

## 9. Hors périmètre (suites identifiées)

- **Profils en paquets satellites** (`PlayBlazor.MudBlazor` & co.) — à reconsidérer une fois les
  trois configs écrites, quand le motif commun sera *observé* et non deviné.
- **Câblage de `XmlDocSummaryReader`** — jamais branché : `AddPlayBlazor` construit
  `new ReflectionCatalogProvider()` sans docs XML. Fluent v5 embarque 1,7 Mo de doc XML,
  MudBlazor aussi : des tooltips gratuits qui dorment. Nécessite de copier le `.xml` en asset
  statique et de le charger au démarrage — assez de travail pour mériter son propre jalon.
- **Le doublon de docs DaisyBlazor** — `phmatray.github.io/daisyblazor/` (repo archivé) et
  `phmatray.github.io/blazor-tailwind-ui/` servent aujourd'hui un contenu **identique au
  caractère près**, chacun se déclarant canonique. L'ancien renvoie 200 et ne pourra plus jamais
  être mis à jour (Actions désactivé sur un repo archivé). Correctif : désarchiver, remplacer le
  contenu Pages par une redirection, ré-archiver. **Autre repo, autre session.**
