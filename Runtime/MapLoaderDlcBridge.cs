#if DLCMANAGER_MLF
using System;
using System.IO;
using UnityEngine;
using MapLoaderFramework.Runtime;

namespace DlcManager.Runtime
{
    /// <summary>
    /// Optional bridge between DlcManager and MapLoaderFramework.
    /// Enable define <c>DLCMANAGER_MLF</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Provides two integration points:
    /// <list type="bullet">
    /// <item><b>DLC gating</b> — when a map or chapter belonging to an unowned DLC pack is loaded,
    /// <see cref="OnDlcGatedMap"/> fires so the game can present a purchase prompt.
    /// This bridge is informational only and does not block loading.</item>
    /// <item><b>Mod support</b> — when <see cref="ModManager"/> is present, subscribes to
    /// <see cref="ModManager.OnModsChanged"/> and reloads DLC pack definitions from the
    /// <c>dlcpacks/</c> subfolder of each enabled mod.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("DlcManager/Map Loader DLC Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderDlcBridge : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when a map or chapter belonging to an unowned DLC pack is loaded.
        /// Parameters: (contentId, packId) — contentId is the map id or <c>"chapter_{N}"</c>.
        /// </summary>
        public System.Action<string, string> OnDlcGatedMap;

        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Reload DLC pack definitions from mod directories when mods change.")]
        [SerializeField] private bool reloadOnModsChanged = true;

        // ─── References ──────────────────────────────────────────────────────────
        private DlcManager _dlc;
        private MapLoaderFramework.Runtime.MapLoaderFramework _framework;
        private ModManager _modManager;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _dlc        = GetComponent<DlcManager>() ?? FindFirstObjectByType<DlcManager>();
            _framework  = GetComponent<MapLoaderFramework.Runtime.MapLoaderFramework>()
                          ?? FindFirstObjectByType<MapLoaderFramework.Runtime.MapLoaderFramework>();
            _modManager = GetComponent<ModManager>() ?? FindFirstObjectByType<ModManager>();

            if (_dlc       == null) Debug.LogWarning("[MapLoaderDlcBridge] DlcManager not found.");
            if (_framework == null) Debug.LogWarning("[MapLoaderDlcBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_framework != null)
            {
                _framework.OnMapLoaded      += OnMapLoaded;
                _framework.OnChapterChanged += OnChapterChanged;
            }
            if (_modManager != null) _modManager.OnModsChanged += OnModsChanged;
        }

        private void OnDisable()
        {
            if (_framework != null)
            {
                _framework.OnMapLoaded      -= OnMapLoaded;
                _framework.OnChapterChanged -= OnChapterChanged;
            }
            if (_modManager != null) _modManager.OnModsChanged -= OnModsChanged;
        }

        // ─── Handlers ────────────────────────────────────────────────────────────
        private void OnMapLoaded(MapData mapData)
        {
            if (_dlc == null || mapData == null) return;
            var pack = _dlc.GetPackForMap(mapData.id);
            if (pack != null && !_dlc.IsOwned(pack.id))
            {
                Debug.LogWarning($"[MapLoaderDlcBridge] Map '{mapData.id}' requires DLC pack '{pack.id}' (not owned).");
                OnDlcGatedMap?.Invoke(mapData.id, pack.id);
            }
        }

        private void OnChapterChanged(int previous, int current)
        {
            if (_dlc == null) return;
            var pack = _dlc.GetPackForChapter(current);
            if (pack != null && !_dlc.IsOwned(pack.id))
            {
                Debug.LogWarning($"[MapLoaderDlcBridge] Chapter {current} requires DLC pack '{pack.id}' (not owned).");
                OnDlcGatedMap?.Invoke($"chapter_{current}", pack.id);
            }
        }

        private void OnModsChanged()
        {
            if (!reloadOnModsChanged || _dlc == null || _modManager == null) return;

            foreach (var (filePath, modId) in _modManager.GetEnabledModDlcPackFiles())
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    _dlc.RegisterPackFromJson(json);
                    Debug.Log($"[MapLoaderDlcBridge] Loaded DLC pack from mod '{modId}': {filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MapLoaderDlcBridge] Failed to load '{filePath}': {ex.Message}");
                }
            }
        }
    }
}
#else
// DLCMANAGER_MLF not defined — bridge is inactive.
namespace DlcManager.Runtime
{
    /// <summary>No-op stub. Enable DLCMANAGER_MLF in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DlcManager/Map Loader DLC Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderDlcBridge : UnityEngine.MonoBehaviour { }
}
#endif

