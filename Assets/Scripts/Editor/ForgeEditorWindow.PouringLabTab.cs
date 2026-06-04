using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Pouring Lab — in-editor pouring simulation sandbox.
    /// Finds runtime MoldController instances and provides state inspection
    /// plus VFX config tweaking. Pour execution is handled via GameManager
    /// (the only MonoBehaviour entry point to DI-resolved services).
    /// </summary>
    public partial class ForgeEditorWindow
    {
        // ── Pouring Lab State ──────────────────────────────────────────────────
        private int _labSourceIndex = 0;
        private int _labTargetIndex = 1;
        private string[] _labMoldNames = Array.Empty<string>();
        private IMoldView[] _labMolds = Array.Empty<IMoldView>();

        // VFX preview
        private float _labPreviewFlowIntensity = 0.8f;
        private float _labPreviewColorBoost = 1.5f;
        private Color _labPreviewColor = new Color(0.95f, 0.72f, 0.05f, 1f);
        private string _labStatus = "Ready.";

        // Mold state inspection
        private int _labInspectIndex;
        private string _labInspectLayersText = "";

        private const string LabEmptyOption = "-- None --";

        private void DrawPouringLabTab()
        {
            EditorGUILayout.LabelField("Pouring Lab", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Runtime check
            if (!UnityEngine.Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Pouring Lab yalnızca Play Mode'da çalışır.\n" +
                    "Enter Play Mode to interact with runtime systems.",
                    MessageType.Info);
                return;
            }

            // Refresh mold references
            if (_labMolds == null || _labMolds.Length == 0 || GUILayout.Button("Refresh Molds"))
            {
                RefreshLabMolds();
            }

            if (_labMolds.Length < 2)
            {
                EditorGUILayout.HelpBox(
                    "Pouring Lab için en az 2 MoldController sahnede aktif olmalı.\n" +
                    "Sahneyi Tools > PuzzleGame > Scene tab'dan oluşturun veya manuel ekleyin.",
                    MessageType.Warning);
                return;
            }

            // ── Mold Selection ─────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Mold Selection", EditorStyles.miniBoldLabel);
                float halfW = (position.width - 8f) * 0.5f;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Source:", GUILayout.Width(50f));
                _labSourceIndex = EditorGUILayout.Popup(_labSourceIndex, _labMoldNames, GUILayout.Width(halfW - 50f));
                EditorGUILayout.LabelField("Target:", GUILayout.Width(50f));
                _labTargetIndex = EditorGUILayout.Popup(_labTargetIndex, _labMoldNames, GUILayout.Width(halfW - 50f));
                EditorGUILayout.EndHorizontal();

                // Quick state display
                if (_labSourceIndex < _labMolds.Length && _labTargetIndex < _labMolds.Length)
                {
                    var src = _labMolds[_labSourceIndex];
                    var tgt = _labMolds[_labTargetIndex];
                    if (src != null && tgt != null)
                    {
                        EditorGUILayout.LabelField(
                            $"  Source: {src.State.LayerCount}/{src.State.MaxLayers} layers  |  " +
                            $"Target: {tgt.State.LayerCount}/{tgt.State.MaxLayers} layers");
                    }
                }
            }

            EditorGUILayout.Space(4);

            // ── VFX Preview ─────────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("VFX Preview", EditorStyles.miniBoldLabel);

                _labPreviewFlowIntensity = EditorGUILayout.Slider("Flow Intensity", _labPreviewFlowIntensity, 0f, 5f);
                _labPreviewColorBoost = EditorGUILayout.Slider("Color Boost", _labPreviewColorBoost, 0.5f, 5f);
                _labPreviewColor = EditorGUILayout.ColorField("Stream Color", _labPreviewColor);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply to VFX Config"))
                {
                    var vfxConfig = Resources.Load<StreamVFXConfig>("Data/StreamVFXConfig");
                    if (vfxConfig != null)
                    {
                        vfxConfig.flowIntensity = _labPreviewFlowIntensity;
                        vfxConfig.colorIntensityBoost = _labPreviewColorBoost;
                        vfxConfig.streamColorHint = _labPreviewColor;
                        EditorUtility.SetDirty(vfxConfig);
                        _labStatus = "VFX config updated.";
                    }
                    else
                    {
                        _labStatus = "StreamVFXConfig not found at Resources/Data/.";
                    }
                }
                if (GUILayout.Button("Load from Config"))
                {
                    var vfxConfig = Resources.Load<StreamVFXConfig>("Data/StreamVFXConfig");
                    if (vfxConfig != null)
                    {
                        _labPreviewFlowIntensity = vfxConfig.flowIntensity;
                        _labPreviewColorBoost = vfxConfig.colorIntensityBoost;
                        _labPreviewColor = vfxConfig.streamColorHint;
                        _labStatus = "Loaded from config.";
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // ── Mold State Inspection ───────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Mold State Inspection", EditorStyles.miniBoldLabel);

                _labInspectIndex = EditorGUILayout.Popup("Mold", _labInspectIndex, _labMoldNames);

                var inspectMold = _labInspectIndex < _labMolds.Length ? _labMolds[_labInspectIndex] : null;
                if (inspectMold != null)
                {
                    var state = inspectMold.State;
                    EditorGUILayout.LabelField($"  Layers: {state.LayerCount}/{state.MaxLayers}");
                    EditorGUILayout.LabelField($"  Is Empty: {state.IsEmpty}");
                    EditorGUILayout.LabelField($"  Is Full: {state.IsFull}");
                    EditorGUILayout.LabelField($"  Is Capped: {inspectMold.IsCapped}");
                    EditorGUILayout.LabelField($"  Mold Index: {inspectMold.MoldIndex}");

                    // Layers breakdown
                    if (state.Layers != null && state.Layers.Count > 0)
                    {
                        _labInspectLayersText = "";
                        for (int i = state.Layers.Count - 1; i >= 0; i--)
                        {
                            var layer = state.Layers[i];
                            _labInspectLayersText += $"[{i}] R:{layer.Color.R:F2} G:{layer.Color.G:F2} B:{layer.Color.B:F2} A:{layer.Color.A:F2} x{layer.Amount:F3}\n";
                        }
                        EditorGUILayout.TextArea(_labInspectLayersText, GUILayout.Height(60));
                    }
                }
            }

            // ── Status ──────────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_labStatus, MessageType.Info);
        }

        private void RefreshLabMolds()
        {
            var controllers = FindObjectsOfType<MoldController>(true);
            _labMolds = controllers.Cast<IMoldView>().ToArray();
            _labMoldNames = _labMolds.Select((m, i) => $"[{i}] {m.GameObject.name}").ToArray();

            if (_labMoldNames.Length == 0)
            {
                _labMoldNames = new[] { LabEmptyOption };
                _labMolds = Array.Empty<IMoldView>();
            }

            _labSourceIndex = Mathf.Clamp(_labSourceIndex, 0, Mathf.Max(0, _labMolds.Length - 1));
            _labTargetIndex = Mathf.Clamp(_labTargetIndex, 0, Mathf.Max(0, _labMolds.Length - 1));
            _labInspectIndex = Mathf.Clamp(_labInspectIndex, 0, Mathf.Max(0, _labMolds.Length - 1));
        }
    }
}
