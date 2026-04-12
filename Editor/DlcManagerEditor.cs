#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DlcManager.Editor
{
    [CustomEditor(typeof(Runtime.DlcManager))]
    public class DlcManagerEditor : UnityEditor.Editor
    {
        // ─── State ───────────────────────────────────────────────────────────────
        private bool _showPacks = true;
        private Vector2 _packsScroll;
        private string _manualPackId = string.Empty;

        // ─── Inspector GUI ────────────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(4);

            var mgr = (Runtime.DlcManager)target;

            DrawLoadedPacksSection(mgr);

            EditorGUILayout.Space(6);
            DrawManualControls(mgr);
        }

        // ─── Sections ────────────────────────────────────────────────────────────
        private void DrawLoadedPacksSection(Runtime.DlcManager mgr)
        {
            _showPacks = EditorGUILayout.Foldout(_showPacks, "Loaded DLC Packs", true);
            if (!_showPacks) return;

            var packs = mgr.GetAllPacks();
            if (packs == null || packs.Count == 0)
            {
                EditorGUILayout.HelpBox("No DLC packs loaded yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("ID",       EditorStyles.miniLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("Owned",    EditorStyles.miniLabel, GUILayout.Width(46));
            EditorGUILayout.LabelField("Maps",     EditorStyles.miniLabel, GUILayout.Width(38));
            EditorGUILayout.LabelField("Chapters", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Actions",  EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            _packsScroll = EditorGUILayout.BeginScrollView(_packsScroll, GUILayout.MaxHeight(180));
            foreach (var pack in packs.Values)
            {
                bool owned = mgr.IsOwned(pack.id);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(pack.id,                              GUILayout.Width(140));
                EditorGUILayout.LabelField(owned ? "✓" : "—",                   GUILayout.Width(46));
                EditorGUILayout.LabelField(pack.mapIds?.Count.ToString()    ?? "0", GUILayout.Width(38));
                EditorGUILayout.LabelField(pack.chapterIds?.Count.ToString() ?? "0", GUILayout.Width(60));

                GUI.enabled = Application.isPlaying;
                if (!owned && GUILayout.Button("Unlock", EditorStyles.miniButton, GUILayout.Width(54)))
                {
                    mgr.Unlock(pack.id);
                    Repaint();
                }
                if (owned && GUILayout.Button("Revoke", EditorStyles.miniButton, GUILayout.Width(54)))
                {
                    mgr.Revoke(pack.id);
                    Repaint();
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawManualControls(Runtime.DlcManager mgr)
        {
            EditorGUILayout.LabelField("Manual Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pack ID", GUILayout.Width(54));
            _manualPackId = EditorGUILayout.TextField(_manualPackId);
            EditorGUILayout.EndHorizontal();

            GUI.enabled = Application.isPlaying && !string.IsNullOrWhiteSpace(_manualPackId);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Unlock", EditorStyles.miniButton))
            {
                mgr.Unlock(_manualPackId);
                Repaint();
            }
            if (GUILayout.Button("Revoke", EditorStyles.miniButton))
            {
                mgr.Revoke(_manualPackId);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to use manual controls.", MessageType.None);
        }
    }
}
#endif
