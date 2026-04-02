#if DLCMANAGER_LM
using UnityEngine;
using LocalizationManager.Runtime;

namespace DlcManager.Runtime
{
    /// <summary>
    /// Optional bridge between DlcManager and LocalizationManager.
    /// Enable define <c>DLCMANAGER_LM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Provides helper methods that resolve localized titles and descriptions for DLC packs.
    /// UI code should call <see cref="GetTitle"/> and <see cref="GetDescription"/> rather than
    /// reading <c>DlcPackData.title</c> directly, so the active language is always applied.
    /// </para>
    /// </summary>
    [AddComponentMenu("DlcManager/Localization DLC Bridge")]
    [DisallowMultipleComponent]
    public class LocalizationDlcBridge : MonoBehaviour
    {
        private DlcManager _dlc;
        private LocalizationManager.Runtime.LocalizationManager _localization;

        private void Awake()
        {
            _dlc          = GetComponent<DlcManager>() ?? FindFirstObjectByType<DlcManager>();
            _localization = GetComponent<LocalizationManager.Runtime.LocalizationManager>()
                            ?? FindFirstObjectByType<LocalizationManager.Runtime.LocalizationManager>();

            if (_dlc          == null) Debug.LogWarning("[LocalizationDlcBridge] DlcManager not found.");
            if (_localization == null) Debug.LogWarning("[LocalizationDlcBridge] LocalizationManager not found.");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the localized title for the DLC pack with the given id.
        /// Falls back to <c>DlcPackData.title</c> when no localization key is set.
        /// </summary>
        public string GetTitle(string packId)
        {
            var data = _dlc?.GetPack(packId);
            return data == null ? string.Empty : Resolve(data.titleLocalizationKey, data.title);
        }

        /// <summary>
        /// Returns the localized title for the given <see cref="DlcPackData"/> object.
        /// </summary>
        public string GetTitle(DlcPackData data)
        {
            if (data == null) return string.Empty;
            return Resolve(data.titleLocalizationKey, data.title);
        }

        /// <summary>
        /// Returns the localized description for the DLC pack with the given id.
        /// Falls back to <c>DlcPackData.description</c> when no localization key is set.
        /// </summary>
        public string GetDescription(string packId)
        {
            var data = _dlc?.GetPack(packId);
            return data == null ? string.Empty : Resolve(data.descriptionLocalizationKey, data.description);
        }

        /// <summary>
        /// Returns the localized description for the given <see cref="DlcPackData"/> object.
        /// </summary>
        public string GetDescription(DlcPackData data)
        {
            if (data == null) return string.Empty;
            return Resolve(data.descriptionLocalizationKey, data.description);
        }

        // ─── Internal ─────────────────────────────────────────────────────────────
        private string Resolve(string locKey, string fallback)
        {
            if (!string.IsNullOrEmpty(locKey) && _localization != null)
                return _localization.GetText(locKey) ?? fallback;
            return fallback;
        }
    }
}
#else
namespace DlcManager.Runtime
{
    /// <summary>No-op stub. Enable DLCMANAGER_LM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("DlcManager/Localization DLC Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class LocalizationDlcBridge : UnityEngine.MonoBehaviour { }
}
#endif
