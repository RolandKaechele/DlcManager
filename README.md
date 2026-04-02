# DlcManager

Runtime DLC gating, pack discovery, and ownership management for Unity 2022.3+.
Supports JSON pack definitions, PlayerPrefs-backed ownership, an optional **MapLoaderFramework** bridge, and an optional **SaveManager** bridge.


## Features

- **JSON Pack Definitions** — load `DlcPackData` from `Resources/DlcPacks/` and/or `persistentDataPath/DlcPacks/`
- **PlayerPrefs Ownership** — zero-setup persistence with `PlayerPrefs`; automatically restored on `Awake`
- **Unlock / Revoke API** — `Unlock(packId)` / `Revoke(packId)` with `OnPackUnlocked` / `OnPackRevoked` events
- **Access Queries** — `IsOwned`, `IsMapAccessible`, `IsChapterAccessible`, `GetPackForMap`, `GetPackForChapter`
- **DlcGate Component** — attach to any scene object to disable/enable it based on pack ownership
- **MapLoaderFramework Bridge** — fires an informational `OnDlcGatedMap` event when a gated map/chapter is loaded; auto-registers mod `dlcpacks/*.json` files when mods change
- **SaveManager Bridge** — persists ownership state inside the current save slot via `SaveManager.SetCustom`
- **LocalizationManager Bridge** — resolve localized pack titles and descriptions via `titleLocalizationKey` / `descriptionLocalizationKey` fields (`DLCMANAGER_LM`)
- **Custom Inspector** — live pack table with Unlock/Revoke buttons during Play Mode


## Installation

1. Open the Unity Package Manager (`Window › Package Manager`).
2. Click **+** › *Add package from Git URL* and enter the repository URL.
3. The `postinstall.js` script will create `Assets/DlcPacks/`, `Assets/Resources/DlcPacks/`, and `Assets/Scripts/` automatically.


## Folder Structure

```
DlcManager/
  Runtime/
    DlcData.cs               — DlcPackData definition
    DlcManager.cs            — Core manager (ownership, queries, events)
    DlcGate.cs               — Scene gate component
    MapLoaderDlcBridge.cs    — MapLoaderFramework bridge (define DLCMANAGER_MLF)
    SaveDlcBridge.cs         — SaveManager bridge (define DLCMANAGER_SM)
    LocalizationDlcBridge.cs — LocalizationManager bridge (define DLCMANAGER_LM)
  Editor/
    DlcManagerEditor.cs      — Custom inspector
  package.json
  postinstall.js
  .gitattributes
  .gitignore
  README.md
```


## Quick Start

### 1. Define a DLC pack (JSON)

Create `Assets/Resources/DlcPacks/season_pass.json`:

```json
{
  "id": "season_pass",
  "title": "Season Pass",
  "description": "Unlocks all premium chapters.",
  "version": "1.0",
  "chapterIds": [10, 11, 12, 13],
  "mapIds": ["world_premium_1", "world_premium_2"],
  "itemIds": []
}
```

### 2. Attach DlcManager to a GameObject

```
GameObject "Managers"
  └── DlcManager (component)
```

### 3. Unlock after a purchase

```csharp
using DlcManager.Runtime;

public class IAPHandler : MonoBehaviour
{
    [SerializeField] private DlcManager dlcManager;

    public void OnPurchaseCompleted(string packId)
    {
        dlcManager.Unlock(packId);
    }
}
```

### 4. Gate a scene object with DlcGate

Attach `DlcGate` to any `GameObject`:

| Field | Description |
| ----- | ----------- |
| Required Pack Id | Pack that must be owned |
| Disable If Not Owned | Disable this GameObject when the pack is not owned |
| On Gated | UnityEvent fired when pack is not owned |

```csharp
// Or check in code:
if (dlcManager.IsChapterAccessible(12))
    LoadChapter12();
```


## DlcPackData JSON Fields

| Field | Type | Description |
| --- | --- | --- |
| `id` | string | Unique pack identifier |
| `title` | string | Display title |
| `description` | string | Short description |
| `titleLocalizationKey` | string | Optional localization key for title |
| `descriptionLocalizationKey` | string | Optional localization key for description |
| `iconResource` | string | `Resources.Load` path for the pack icon |
| `version` | string | Pack version string |
| `mapIds` | string[] | Map IDs included in this pack |
| `chapterIds` | int[] | Chapter numbers included in this pack |
| `itemIds` | string[] | Item IDs granted by this pack |
| `cutsceneIds` | string[] | Cutscene IDs included in this pack |


## JSON File Locations

| Source | Path | Notes |
| --- | --- | --- |
| Resources | `Assets/Resources/DlcPacks/*.json` | Bundled with the game |
| Persistent | `Application.persistentDataPath/DlcPacks/*.json` | Downloaded / side-loaded |

Toggle `Load From Persistent Data Path` in the inspector to enable persistent-path loading.


## Runtime API

### DlcManager

| Member | Type | Description |
| --- | --- | --- |
| `Unlock(packId)` | void | Marks a pack as owned; fires `OnPackUnlocked` |
| `Revoke(packId)` | void | Removes ownership; fires `OnPackRevoked` |
| `IsOwned(packId)` | bool | Returns true if the pack is owned |
| `IsMapAccessible(mapId)` | bool | True if mapId belongs to an owned pack (or no pack) |
| `IsChapterAccessible(chapterId)` | bool | True if chapter belongs to an owned pack (or no pack) |
| `GetPackForMap(mapId)` | DlcPackData? | Pack that contains the map, or null |
| `GetPackForChapter(chapterId)` | DlcPackData? | Pack that contains the chapter, or null |
| `GetPack(packId)` | DlcPackData? | Look up a pack by id |
| `GetAllPacks()` | List\<DlcPackData\> | All loaded packs |
| `GetOwnedPackIds()` | IEnumerable\<string\> | IDs of all currently owned packs |
| `OnPackUnlocked` | event Action\<string\> | Fired when a pack is unlocked |
| `OnPackRevoked` | event Action\<string\> | Fired when a pack is revoked |

### DlcGate

| Member | Description |
| --- | --- |
| `IsAccessible()` | Returns true if the required pack is owned |
| `Evaluate()` | Re-evaluates and applies `disableIfNotOwned` logic |
| `OnGated` | `Action<string>` callback fired with packId when not owned |


## MapLoaderFramework Integration

**Define:** `DLCMANAGER_MLF`

Attach `MapLoaderDlcBridge` alongside `DlcManager`. When a map or chapter is loaded, the bridge checks whether the content belongs to an unowned pack and fires an **informational** event — it does **not** block loading.

```csharp
bridge.OnDlcGatedMap += (contentId, packId) =>
{
    Debug.Log($"Content '{contentId}' requires pack '{packId}' which is not owned.");
};
```

| Inspector Field | Default | Description |
| --- | --- | --- |
| `Reload On Mods Changed` | `true` | Re-register DLC pack definitions from enabled mod `dlcpacks/` subfolders when mods change |

Mod DLC JSON files are loaded from each mod's `dlcpacks/` subfolder (e.g. `Mods/my_mod/dlcpacks/vip_pack.json`). Declare them in `mod_manifest.json` under `dlc_pack_files`.

> **Note:** The bridge is informational only. To block players from accessing gated content, use `DlcGate` components or guard checks with `IsMapAccessible` / `IsChapterAccessible`.


## SaveManager Integration

**Define:** `DLCMANAGER_SM`

Attach `SaveDlcBridge` alongside `DlcManager` and `SaveManager`. Ownership state will be serialised into the active save slot in addition to PlayerPrefs.

| Method | Description |
| --- | --- |
| `SaveOwnership()` | Write all owned pack ids to the current save slot |
| `LoadOwnership()` | Read and restore owned pack ids from the current save slot |

Toggle `Auto Save On Change` to automatically call `SaveOwnership()` whenever a pack is unlocked or revoked.

```csharp
// On new game / load:
saveDlcBridge.LoadOwnership();

// After IAP:
dlcManager.Unlock("season_pass");   // auto-saves when autoSaveOnChange = true
```

> **Ownership is additive.** PlayerPrefs ownership is always active. `SetOwnedFromSnapshot` adds any pack ids from the save slot on top of the PlayerPrefs-restored set.


## LocalizationManager Integration

**Define:** `DLCMANAGER_LM`

Attach `LocalizationDlcBridge` to any GameObject. It exposes helpers that resolve localized strings from `LocalizationManager` and fall back to the raw field values when no key is set.

```csharp
var bridge = FindFirstObjectByType<LocalizationDlcBridge>();

// Look up by pack id
string title = bridge.GetTitle("season_pass");
string desc  = bridge.GetDescription("season_pass");

// Or pass the data object directly
string title2 = bridge.GetTitle(data);
```

| Method | Description |
| --- | --- |
| `GetTitle(string packId)` | Localized title for the pack, or raw `title` as fallback |
| `GetTitle(DlcPackData data)` | Same, given a `DlcPackData` object |
| `GetDescription(string packId)` | Localized description, or raw `description` as fallback |
| `GetDescription(DlcPackData data)` | Same, given a `DlcPackData` object |


## Integration Defines Summary

| Define | Bridge | Description |
| --- | --- | --- |
| `DLCMANAGER_MLF` | `MapLoaderDlcBridge` | Informational event when gated content is loaded; mod `dlcpacks/` reload support |
| `DLCMANAGER_SM` | `SaveDlcBridge` | Persist DLC ownership inside SaveManager save slot |
| `DLCMANAGER_LM` | `LocalizationDlcBridge` | Localized title/description lookup via LocalizationManager |

Set defines under **Edit › Project Settings › Player › Scripting Define Symbols**.


## Dependencies

| Package | Required | Purpose |
| --- | --- | --- |
| Unity 2022.3+ | ✓ | Engine |
| MapLoaderFramework | Optional | `DLCMANAGER_MLF` bridge |
| SaveManager | Optional | `DLCMANAGER_SM` bridge |
| LocalizationManager | Optional | `DLCMANAGER_LM` bridge |


## Repository

Standalone Git repository. Install via Unity Package Manager using the Git URL.


## License

MIT — see `LICENSE` for details.
