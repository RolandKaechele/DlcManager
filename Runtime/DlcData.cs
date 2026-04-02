using System;
using System.Collections.Generic;
using UnityEngine;

namespace DlcManager.Runtime
{
    // -------------------------------------------------------------------------
    // DlcPackData
    // -------------------------------------------------------------------------

    /// <summary>
    /// Describes a single DLC pack that gates content behind an ownership check.
    /// Authored in JSON and stored in <c>Resources/DlcPacks/</c>.
    /// </summary>
    [Serializable]
    public class DlcPackData
    {
        /// <summary>Unique identifier for this DLC pack.</summary>
        public string id;

        /// <summary>Human-readable pack title.</summary>
        public string title;

        /// <summary>Short description shown in the store or DLC menu.</summary>
        public string description;

        /// <summary>Localization key for the title.</summary>
        public string titleLocalizationKey;

        /// <summary>Localization key for the description.</summary>
        public string descriptionLocalizationKey;

        /// <summary>Resources-relative path to a pack icon sprite.</summary>
        public string iconResource;

        /// <summary>Pack version string, e.g. <c>"1.0.0"</c>.</summary>
        public string version;

        /// <summary>
        /// Map ids covered by this pack.
        /// Any map listed here requires this pack to be owned before access is granted.
        /// </summary>
        public List<string> mapIds;

        /// <summary>
        /// Chapter ids covered by this pack.
        /// Any chapter listed here requires this pack to be owned before access is granted.
        /// </summary>
        public List<int> chapterIds;

        /// <summary>Item ids that become available when this pack is owned.</summary>
        public List<string> itemIds;

        /// <summary>Cutscene sequence ids included in this pack.</summary>
        public List<string> cutsceneIds;

        /// <summary>Raw JSON stored during deserialisation (non-serialised).</summary>
        [NonSerialized] public string rawJson;
    }
}
