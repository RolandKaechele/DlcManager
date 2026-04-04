using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DlcManager.Runtime
{
    /// <summary>
    /// <b>DlcManager</b> manages DLC pack definitions and ownership state.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Load <see cref="DlcPackData"/> definitions from <c>Resources/DlcPacks/</c> and an optional external folder.</item>
    /// <item>Persist and restore ownership state via PlayerPrefs (no SaveManager required for the base case).</item>
    /// <item>Expose a simple <see cref="Unlock"/> / <see cref="Revoke"/> / <see cref="IsOwned"/> API.</item>
    /// <item>Answer content-access queries: <see cref="IsMapAccessible"/>, <see cref="IsChapterAccessible"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to a persistent manager GameObject. Place DLC pack JSON files in
    /// <c>Assets/Resources/DlcPacks/</c>. Call <see cref="Unlock"/> when an in-app purchase completes.
    /// </para>
    /// </summary>
    [AddComponentMenu("DlcManager/DLC Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class DlcManager : SerializedMonoBehaviour
#else
    public class DlcManager : MonoBehaviour
#endif
    {
        // ─── Constants ───────────────────────────────────────────────────────────
        private const string PrefKeyPrefix = "DLC_Owned_";

        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("If true, DLC pack definitions are also loaded from persistentDataPath/DlcPacks/.")]
        [SerializeField] private bool loadFromPersistentDataPath = true;

        [Header("Loaded packs (read-only, set at runtime)")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField] private List<string> loadedPackIds = new List<string>();

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired when a pack is unlocked. Parameter: pack id.</summary>
        public event Action<string> OnPackUnlocked;

        /// <summary>Fired when a pack is revoked. Parameter: pack id.</summary>
        public event Action<string> OnPackRevoked;

        // ─── State ───────────────────────────────────────────────────────────────
        private readonly Dictionary<string, DlcPackData> _packs = new Dictionary<string, DlcPackData>();
        private readonly HashSet<string>                 _owned = new HashSet<string>();

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            LoadAllPacks();
            RestoreOwnershipFromPrefs();
        }

        // ─── JSON Loading ─────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all DLC pack JSON files from <c>Resources/DlcPacks/</c> and the external folder.
        /// Call again at runtime to reload after hot-patches.
        /// </summary>
        public void LoadAllPacks()
        {
            _packs.Clear();
            loadedPackIds.Clear();

            var assets = Resources.LoadAll<TextAsset>("DlcPacks");
            foreach (var a in assets)
                RegisterFromJson(a.text);

            if (loadFromPersistentDataPath)
            {
                string dir = Path.Combine(Application.persistentDataPath, "DlcPacks");
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                    {
                        try { RegisterFromJson(File.ReadAllText(f)); }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[DlcManager] Failed to load {f}: {ex.Message}");
                        }
                    }
                }
            }

            Debug.Log($"[DlcManager] Loaded {_packs.Count} DLC pack definition(s).");
        }

        /// <summary>
        /// Registers a single DLC pack definition from a raw JSON string.
        /// Safe to call at runtime (e.g. from mod-loading bridges) to add or replace a pack definition.
        /// </summary>
        public void RegisterPackFromJson(string json)
        {
            RegisterFromJson(json);
        }

        private void RegisterFromJson(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<DlcPackData>(json);
                if (data == null || string.IsNullOrEmpty(data.id)) return;
                data.rawJson = json;
                _packs[data.id] = data;
                if (!loadedPackIds.Contains(data.id))
                    loadedPackIds.Add(data.id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DlcManager] Failed to parse DLC pack JSON: {ex.Message}");
            }
        }

        private void RestoreOwnershipFromPrefs()
        {
            _owned.Clear();
            foreach (var id in _packs.Keys)
            {
                if (PlayerPrefs.GetInt(PrefKeyPrefix + id, 0) == 1)
                    _owned.Add(id);
            }
        }

        // ─── Ownership API ────────────────────────────────────────────────────────

        /// <summary>
        /// Grant ownership of the DLC pack with the given id.
        /// Persists to PlayerPrefs and fires <see cref="OnPackUnlocked"/>.
        /// </summary>
        public void Unlock(string packId)
        {
            if (!_packs.ContainsKey(packId))
            {
                Debug.LogWarning($"[DlcManager] Pack '{packId}' not found.");
                return;
            }

            if (_owned.Add(packId))
            {
                PlayerPrefs.SetInt(PrefKeyPrefix + packId, 1);
                PlayerPrefs.Save();
                OnPackUnlocked?.Invoke(packId);
                Debug.Log($"[DlcManager] Pack '{packId}' unlocked.");
            }
        }

        /// <summary>
        /// Revoke ownership of the DLC pack with the given id.
        /// Persists to PlayerPrefs and fires <see cref="OnPackRevoked"/>.
        /// </summary>
        public void Revoke(string packId)
        {
            if (_owned.Remove(packId))
            {
                PlayerPrefs.SetInt(PrefKeyPrefix + packId, 0);
                PlayerPrefs.Save();
                OnPackRevoked?.Invoke(packId);
                Debug.Log($"[DlcManager] Pack '{packId}' revoked.");
            }
        }

        // ─── Query ────────────────────────────────────────────────────────────────

        /// <summary>Returns true if the pack with the given id is currently owned.</summary>
        public bool IsOwned(string packId) => _owned.Contains(packId);

        /// <summary>
        /// Returns true if the map is accessible — either no pack gates it, or the gating pack is owned.
        /// </summary>
        public bool IsMapAccessible(string mapId)
        {
            var gatePack = _packs.Values.FirstOrDefault(p => p.mapIds != null && p.mapIds.Contains(mapId));
            return gatePack == null || _owned.Contains(gatePack.id);
        }

        /// <summary>
        /// Returns true if the chapter is accessible — either no pack gates it, or the gating pack is owned.
        /// </summary>
        public bool IsChapterAccessible(int chapterId)
        {
            var gatePack = _packs.Values.FirstOrDefault(p => p.chapterIds != null && p.chapterIds.Contains(chapterId));
            return gatePack == null || _owned.Contains(gatePack.id);
        }

        /// <summary>Returns the DLC pack that gates the given map id, or null if none.</summary>
        public DlcPackData GetPackForMap(string mapId) =>
            _packs.Values.FirstOrDefault(p => p.mapIds != null && p.mapIds.Contains(mapId));

        /// <summary>Returns the DLC pack that gates the given chapter id, or null if none.</summary>
        public DlcPackData GetPackForChapter(int chapterId) =>
            _packs.Values.FirstOrDefault(p => p.chapterIds != null && p.chapterIds.Contains(chapterId));

        /// <summary>Returns the <see cref="DlcPackData"/> for the given id, or null.</summary>
        public DlcPackData GetPack(string packId) =>
            _packs.TryGetValue(packId, out var p) ? p : null;

        /// <summary>Returns all loaded DLC pack definitions.</summary>
        public IReadOnlyDictionary<string, DlcPackData> GetAllPacks() => _packs;

        /// <summary>Returns all currently owned pack ids.</summary>
        public IReadOnlyCollection<string> GetOwnedPackIds() => _owned;

        // ─── Internal (SaveDlcBridge) ─────────────────────────────────────────────

        /// <summary>
        /// Overwrite the in-memory ownership set from a snapshot (called by SaveDlcBridge on load).
        /// Does not persist to PlayerPrefs — call <see cref="Unlock"/> per pack to persist there too.
        /// </summary>
        internal void SetOwnedFromSnapshot(IEnumerable<string> packIds)
        {
            _owned.Clear();
            foreach (var id in packIds)
                if (_packs.ContainsKey(id))
                    _owned.Add(id);
        }
    }
}
