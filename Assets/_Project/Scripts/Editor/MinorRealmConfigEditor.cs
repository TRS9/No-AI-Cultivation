using UnityEngine;
using UnityEditor;
using CultivationGame.Data;

namespace CultivationGame.Editor
{
    [CustomEditor(typeof(MinorRealmConfig))]
    public class MinorRealmConfigEditor : UnityEditor.Editor
    {
        // ── Foldout state ──────────────────────────────────────────────────────
        private bool _showShape      = true;
        private bool _showTerrain    = true;
        private bool _showWater      = true;
        private bool _showCaves      = true;
        private bool _showMultiBiome = true;
        private bool _showDeco       = true;
        private bool _showPOI        = true;
        private bool _showEssences   = true;
        private bool _showSmoothing  = true;
        private bool _showSlope      = true;
        private bool _showAtmosphere = true;

        // ── Section colours ────────────────────────────────────────────────────
        private static readonly Color ColIdentity   = Hex(0x3A6B8A);
        private static readonly Color ColShape      = Hex(0x3D7A4A);
        private static readonly Color ColTerrain    = Hex(0x7A5C3A);
        private static readonly Color ColWater      = Hex(0x2A5F8A);
        private static readonly Color ColCaves      = Hex(0x5C3A7A);
        private static readonly Color ColMulti      = Hex(0x8A5A2A);
        private static readonly Color ColDeco       = Hex(0x3A7A5C);
        private static readonly Color ColPOI        = Hex(0x7A6A2A);
        private static readonly Color ColEssences   = Hex(0x6A3A8A);
        private static readonly Color ColSmoothing  = Hex(0x4A6A7A);
        private static readonly Color ColSlope      = Hex(0x7A4A3A);
        private static readonly Color ColAtmosphere = Hex(0x2A6A7A);

        private static Color Hex(uint rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >>  8) & 0xFF) / 255f,
            ((rgb >>  0) & 0xFF) / 255f, 1f);

        // ── Cached styles ──────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;
        private bool     _stylesReady;

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = Color.white },
                padding   = new RectOffset(4, 0, 0, 0),
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(0.75f, 0.9f, 1f) },
            };

            _boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 6, 8),
                margin  = new RectOffset(0, 0, 0, 4),
            };

            _stylesReady = true;
        }

        // ══════════════════════════════════════════════════════════════════════
        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            // ── Identity ──────────────────────────────────────────────────────
            SectionHeader("  IDENTITY", ColIdentity);
            EditorGUILayout.BeginVertical(_boxStyle);
            Prop("biome", "Biome Type");
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);

            // ── Terrain Shape ─────────────────────────────────────────────────
            _showShape = SectionFoldout("  TERRAIN SHAPE", ColShape, _showShape);
            if (_showShape)
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                var sp = serializedObject.FindProperty("shapeProfile");

                Prop(sp, "octaves",        "Noise Octaves",   "Octaves that build the base heightmap. Empty = default 3-octave Perlin.");
                GUILayout.Space(4);
                Prop(sp, "ridgeWeight",    "Ridge Weight",    "0 = pure Perlin, 1 = sharp mountain ridges (inverted Perlin).");
                GUILayout.Space(4);
                Prop(sp, "heightContrast", "Height Contrast", ">1 = deeper valleys + taller peaks. <1 = flatter. 0 = treated as 1.");
                Prop(sp, "heightFloor",    "Height Floor",    "Normalized floor — terrain below this is clamped up.");
                Prop(sp, "heightCeiling",  "Height Ceiling",  "Normalized ceiling. 0 = no ceiling.");
                GUILayout.Space(4);

                var steps = sp.FindPropertyRelative("terraceSteps");
                Prop(sp, "terraceSteps", "Terrace Steps", "0 = no terracing. Higher = more cliff steps (mesas, plateaus).");
                if (steps.intValue > 0)
                {
                    EditorGUI.indentLevel++;
                    Prop(sp, "terraceSharpness", "Sharpness", "0 = smooth ramps, 1 = hard cliff edges.");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Terrain ───────────────────────────────────────────────────────
            _showTerrain = SectionFoldout("  TERRAIN", ColTerrain, _showTerrain);
            if (_showTerrain)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                Prop("terrainLayers", "Terrain Layers");
                Prop("maxHeight",     "Max Height (m)", "World-space height at normalized 1.0. Controls how tall mountains get.");
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Water ─────────────────────────────────────────────────────────
            _showWater = SectionFoldout("  WATER", ColWater, _showWater);
            if (_showWater)
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                var wp      = serializedObject.FindProperty("water");
                var enabled = wp.FindPropertyRelative("enabled");
                Prop(wp, "enabled",     "Enable Water");
                Prop(wp, "waterPrefab", "Water Prefab");

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    GUILayout.Space(4);
                    SubHeader("Sea Level");
                    EditorGUI.indentLevel++;
                    Prop(wp, "waterLevel", "Water Level", "Normalized height (0-1 of maxHeight) of the water plane.");
                    EditorGUI.indentLevel--;

                    GUILayout.Space(4);
                    SubHeader("Beach");
                    EditorGUI.indentLevel++;
                    Prop(wp, "beachBandWidth",      "Band Width",       "Width of the flattening zone above water level.");
                    Prop(wp, "beachFlattenStrength", "Flatten Strength", "How much the beach band flattens. 0 = none, 1 = fully flat.");
                    EditorGUI.indentLevel--;

                    GUILayout.Space(4);
                    SubHeader("Lakes");
                    EditorGUI.indentLevel++;
                    var lakeCount = wp.FindPropertyRelative("lakeCount");
                    Prop(wp, "lakeCount", "Lake Count", "Bowl-shaped basins carved into the terrain interior.");
                    if (lakeCount.intValue > 0)
                    {
                        Prop(wp, "lakeRadius", "Radius", "Fraction of map size. 0.05 = small pond, 0.2 = large lake.");
                        Prop(wp, "lakeDepth",  "Depth",  "How far below water level the lake bottom sits.");
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Caves ─────────────────────────────────────────────────────────
            _showCaves = SectionFoldout("  CAVES", ColCaves, _showCaves);
            if (_showCaves)
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                var cp    = serializedObject.FindProperty("caves");
                var count = cp.FindPropertyRelative("caveCount");
                Prop(cp, "cavePrefabs", "Cave Prefabs");
                Prop(cp, "caveCount",   "Cave Count");

                if (count.intValue > 0)
                {
                    GUILayout.Space(4);
                    SubHeader("Placement Constraints");
                    EditorGUI.indentLevel++;
                    Prop(cp, "minSlopeDegrees", "Min Slope (deg)", "Minimum terrain slope angle for a valid cave entry.");
                    Prop(cp, "maxSlopeDegrees", "Max Slope (deg)", "Maximum terrain slope angle for a valid cave entry.");
                    Prop(cp, "minHeight",        "Min Height",     "Normalized minimum height for placement.");
                    Prop(cp, "maxHeight",        "Max Height",     "Normalized maximum height for placement.");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Multi-Biome ───────────────────────────────────────────────────
            _showMultiBiome = SectionFoldout("  MULTI-BIOME", ColMulti, _showMultiBiome);
            if (_showMultiBiome)
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                var mp    = serializedObject.FindProperty("multiBiome");
                var allow = mp.FindPropertyRelative("allowMultiBiome");
                Prop(mp, "allowMultiBiome", "Allow Multi-Biome");

                using (new EditorGUI.DisabledScope(!allow.boolValue))
                {
                    Prop(mp, "multiBiomeChance", "Trigger Chance",   "Probability (0-1) this realm rolls as multi-biome on generation.");
                    Prop(mp, "secondaryBiomes",  "Secondary Biomes", "Biome types that appear as Voronoi zones alongside the primary.");
                    GUILayout.Space(4);
                    SubHeader("Voronoi");
                    EditorGUI.indentLevel++;
                    Prop(mp, "voronoiCellCount", "Cell Count",   "More cells = smaller, more fragmented zones. Typically 4-6.");
                    Prop(mp, "blendRadius",      "Blend Radius", "Blend zone width at biome boundaries, in heightmap texels. Higher = smoother.");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Decorations ───────────────────────────────────────────────────
            _showDeco = SectionFoldout("  DECORATIONS", ColDeco, _showDeco);
            if (_showDeco)
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                SubHeader("Prefabs");
                EditorGUI.indentLevel++;
                Prop("decorationPrefabs", "General Prefabs", "Placed anywhere, height-independent (30% mixed in when groups also exist).");
                Prop("decorationGroups",  "Height Groups",   "Each group restricts placement to a normalized height range.");
                EditorGUI.indentLevel--;

                GUILayout.Space(4);
                SubHeader("Budget & Spacing");
                EditorGUI.indentLevel++;
                Prop("decorationCount", "Count",       "Total decorations for this biome (each biome gets its own budget in multi-biome).");
                Prop("minDecorSpacing", "Min Spacing", "Minimum world-space distance between any two decorations.");
                EditorGUI.indentLevel--;

                GUILayout.Space(4);
                SubHeader("Clustering");
                EditorGUI.indentLevel++;
                Prop("clusterSize",   "Cluster Size",   "Max decorations spawned together around one anchor point.");
                Prop("clusterSpread", "Cluster Spread", "Radius within which cluster members scatter from the anchor.");
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Points of Interest ────────────────────────────────────────────
            _showPOI = SectionFoldout("  POINTS OF INTEREST", ColPOI, _showPOI);
            if (_showPOI)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                Prop("poiCount",     "POI Count",        "Number of POI ring groups generated.");
                Prop("poiSpacing",   "Min Spacing",      "Minimum distance between POI ring centres.");
                Prop("poiRingCount", "Objects Per Ring", "Decoration objects placed in each POI ring.");
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Essences ──────────────────────────────────────────────────────
            _showEssences = SectionFoldout("  ESSENCES", ColEssences, _showEssences);
            if (_showEssences)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                Prop("essencePrefabs", "Essence Prefabs");
                Prop("essenceCount",   "Count", "Number of essences scattered across the realm.");
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Heightmap Smoothing ────────────────────────────────────────────
            _showSmoothing = SectionFoldout("  HEIGHTMAP SMOOTHING", ColSmoothing, _showSmoothing);
            if (_showSmoothing)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                Prop("smoothingIterations",       "Iterations",           "Box-blur passes on the final heightmap. Higher = smoother terrain, 0 = raw.");
                Prop("postTerraceSmoothIterations","Post-Terrace Smooth",  "Extra blur passes after terracing to soften hard cliff edges.");
                GUILayout.Space(4);
                SubHeader("Edge Falloff");
                EditorGUI.indentLevel++;
                Prop("edgeFalloffWidth", "Falloff Width", "Fraction of terrain that ramps up at map edges (0.05-0.5). Larger = more gradual boundary.");
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Slope Texturing ───────────────────────────────────────────────
            _showSlope = SectionFoldout("  SLOPE TEXTURING", ColSlope, _showSlope);
            if (_showSlope)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                Prop("cliffTerrainLayer",    "Cliff Layer",        "TerrainLayer painted on steep slopes (e.g. TL_Rock).");
                GUILayout.Space(4);
                SubHeader("Thresholds");
                EditorGUI.indentLevel++;
                Prop("slopeTextureThreshold","Threshold (deg)",   "Slope angle where cliff texture starts blending in.");
                Prop("slopeBlendRange",       "Blend Range (deg)", "Degrees over which the blend transitions from base to cliff texture.");
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(2);

            // ── Atmosphere ────────────────────────────────────────────────────
            _showAtmosphere = SectionFoldout("  ATMOSPHERE", ColAtmosphere, _showAtmosphere);
            if (_showAtmosphere)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                Prop("fogColor",    "Fog Colour");
                Prop("fogDensity",  "Fog Density");
                Prop("ambientLight","Ambient Light");
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(4);
            serializedObject.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════════

        private void SectionHeader(string label, Color col)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, col * 0.75f);
            rect.xMin += 2;
            GUI.Label(rect, label, _headerStyle);
            GUILayout.Space(2);
        }

        private bool SectionFoldout(string label, Color col, bool foldout)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, col * 0.75f);
            rect.xMin  += 2;
            rect.width -= 2;
            bool result = EditorGUI.Foldout(rect, foldout, label, true, _headerStyle);
            GUILayout.Space(2);
            return result;
        }

        private void SubHeader(string text)
        {
            GUILayout.Label(text, _subHeaderStyle);
        }

        private void Prop(string name, string label = null, string tooltip = null)
        {
            var p = serializedObject.FindProperty(name);
            if (p == null) { EditorGUILayout.HelpBox($"Property '{name}' not found.", MessageType.Warning); return; }
            EditorGUILayout.PropertyField(p, MakeLabel(label ?? p.displayName, tooltip ?? p.tooltip), true);
        }

        private void Prop(SerializedProperty parent, string name, string label = null, string tooltip = null)
        {
            var p = parent.FindPropertyRelative(name);
            if (p == null) { EditorGUILayout.HelpBox($"Property '{name}' not found.", MessageType.Warning); return; }
            EditorGUILayout.PropertyField(p, MakeLabel(label ?? p.displayName, tooltip ?? p.tooltip), true);
        }

        private static GUIContent MakeLabel(string text, string tooltip) =>
            string.IsNullOrEmpty(tooltip) ? new GUIContent(text) : new GUIContent(text, tooltip);
    }
}
