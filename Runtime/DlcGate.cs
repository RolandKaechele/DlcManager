using UnityEngine;

namespace DlcManager.Runtime
{
    /// <summary>
    /// Scene component that gates access to a GameObject based on DLC pack ownership.
    /// If the required pack is not owned the GameObject is optionally disabled and
    /// <see cref="OnGated"/> is fired so UI can present a purchase prompt.
    /// </summary>
    [AddComponentMenu("DlcManager/DLC Gate")]
    public class DlcGate : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Pack id that must be owned to pass this gate. Leave empty to always allow.")]
        [SerializeField] private string requiredPackId;

        [Tooltip("Disable (deactivate) this GameObject if the required DLC is not owned.")]
        [SerializeField] private bool disableIfNotOwned = true;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the gate is evaluated and the required DLC is not owned.
        /// Parameter: the pack id that is missing.
        /// Use this to show a purchase prompt.
        /// </summary>
        public System.Action<string> OnGated;

        // ─── Internal ────────────────────────────────────────────────────────────
        private DlcManager _dlc;

        private void Start()
        {
            _dlc = FindFirstObjectByType<DlcManager>();
            if (_dlc == null)
                Debug.LogWarning("[DlcGate] No DlcManager found in scene.");
            Evaluate();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Re-evaluate gate access. Call this after <see cref="DlcManager.Unlock"/> or
        /// <see cref="DlcManager.Revoke"/> to refresh the GameObject's active state.
        /// </summary>
        public void Evaluate()
        {
            if (_dlc == null || string.IsNullOrEmpty(requiredPackId)) return;

            bool owned = _dlc.IsOwned(requiredPackId);

            if (!owned)
                OnGated?.Invoke(requiredPackId);

            if (disableIfNotOwned)
                gameObject.SetActive(owned);
        }

        /// <summary>Returns true if the required DLC pack is currently owned (or no pack is required).</summary>
        public bool IsAccessible() =>
            _dlc == null || string.IsNullOrEmpty(requiredPackId) || _dlc.IsOwned(requiredPackId);
    }
}
