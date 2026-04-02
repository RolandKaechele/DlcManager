#if DLCMANAGER_SM
using System;
using System.Collections.Generic;
using UnityEngine;
using SaveManager.Runtime;

namespace DlcManager.Runtime
{
    /// <summary>
    /// Optional bridge between DlcManager and SaveManager.
    /// Enable define <c>DLCMANAGER_SM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Persists DLC ownership state as a JSON blob inside the SaveManager save slot so
    /// ownership follows the player's save file rather than the device alone.
    /// Note: PlayerPrefs backing in DlcManager still applies — the two sources are additive.
    /// </para>
    /// </summary>
    [AddComponentMenu("DlcManager/Save DLC Bridge")]
    [DisallowMultipleComponent]
    public class SaveDlcBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Custom data key used inside the SaveManager save slot.")]
        [SerializeField] private string saveKey = "dlc_ownership";

        [Tooltip("Automatically persist ownership state whenever a pack is unlocked or revoked.")]
        [SerializeField] private bool autoSaveOnChange = true;

        // ─── References ──────────────────────────────────────────────────────────
        private DlcManager _dlc;
        private SaveManager.Runtime.SaveManager _save;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _dlc  = GetComponent<DlcManager>() ?? FindFirstObjectByType<DlcManager>();
            _save = GetComponent<SaveManager.Runtime.SaveManager>()
                    ?? FindFirstObjectByType<SaveManager.Runtime.SaveManager>();

            if (_dlc  == null) { Debug.LogWarning("[SaveDlcBridge] DlcManager not found."); return; }
            if (_save == null) { Debug.LogWarning("[SaveDlcBridge] SaveManager not found."); return; }
        }

        private void OnEnable()
        {
            if (_dlc != null)
            {
                _dlc.OnPackUnlocked += OnOwnershipChanged;
                _dlc.OnPackRevoked  += OnOwnershipChanged;
            }
        }

        private void OnDisable()
        {
            if (_dlc != null)
            {
                _dlc.OnPackUnlocked -= OnOwnershipChanged;
                _dlc.OnPackRevoked  -= OnOwnershipChanged;
            }
        }

        // ─── Handlers ────────────────────────────────────────────────────────────
        private void OnOwnershipChanged(string packId)
        {
            if (autoSaveOnChange) SaveOwnership();
        }

        // ─── Persistence ─────────────────────────────────────────────────────────

        /// <summary>Write all owned pack ids to the active save slot.</summary>
        public void SaveOwnership()
        {
            if (_save == null || _dlc == null) return;
            var snapshot = new DlcOwnershipSnapshot();
            snapshot.ownedPackIds.AddRange(_dlc.GetOwnedPackIds());
            _save.SetCustom(saveKey, JsonUtility.ToJson(snapshot));
        }

        /// <summary>Restore owned pack ids from the active save slot into DlcManager.</summary>
        public void LoadOwnership()
        {
            if (_save == null || _dlc == null) return;
            string json = _save.GetCustom(saveKey);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var snapshot = JsonUtility.FromJson<DlcOwnershipSnapshot>(json);
                if (snapshot?.ownedPackIds == null) return;
                _dlc.SetOwnedFromSnapshot(snapshot.ownedPackIds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDlcBridge] Failed to load ownership: {ex.Message}");
            }
        }
    }

    [Serializable]
    internal class DlcOwnershipSnapshot
    {
        public List<string> ownedPackIds = new List<string>();
    }
}
#else
// DLCMANAGER_SM not defined — bridge is inactive.
namespace DlcManager.Runtime
{
    /// <summary>No-op stub. Enable DLCMANAGER_SM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DlcManager/Save DLC Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class SaveDlcBridge : UnityEngine.MonoBehaviour { }
}
#endif
