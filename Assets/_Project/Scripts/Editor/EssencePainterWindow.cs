using UnityEngine;
using UnityEditor;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.Editor
{
    public class EssencePainterWindow : EditorWindow
    {
        private EssenceData selectedEssence;
        private GameObject essencePrefab;
        private float brushSpacing = 2f;
        private bool isPainting;

        private Vector3 lastPlacedPosition;
        private bool hasPlacedFirst;

        [MenuItem("Tools/Essence Painter")]
        public static void Open() => GetWindow<EssencePainterWindow>("Essence Painter");

        private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            isPainting = false;
        }

        private void OnGUI()
        {
            GUILayout.Label("Essence Painter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            selectedEssence = (EssenceData)EditorGUILayout.ObjectField("Essence Data", selectedEssence, typeof(EssenceData), false);
            essencePrefab = (GameObject)EditorGUILayout.ObjectField("Essence Prefab", essencePrefab, typeof(GameObject), false);
            brushSpacing = EditorGUILayout.FloatField("Min Spacing", brushSpacing);

            EditorGUILayout.Space();

            if (selectedEssence == null || essencePrefab == null)
            {
                EditorGUILayout.HelpBox("Assign an Essence Data and a Prefab to start painting.", MessageType.Info);
                return;
            }

            var label = isPainting ? "Stop Painting" : "Start Painting";
            var color = isPainting ? Color.red : Color.green;
            GUI.backgroundColor = color;
            if (GUILayout.Button(label, GUILayout.Height(40)))
            {
                isPainting = !isPainting;
                hasPlacedFirst = false;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if (isPainting)
                EditorGUILayout.HelpBox("Hold LMB and drag in the Scene view to paint essences.", MessageType.None);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!isPainting || selectedEssence == null || essencePrefab == null) return;

            var evt = Event.current;
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            // Draw brush circle
            Handles.color = new Color(0f, 1f, 0.5f, 0.5f);
            Handles.DrawWireDisc(hit.point, hit.normal, brushSpacing * 0.5f);
            sceneView.Repaint();

            bool isLeftMouse = evt.button == 0;
            bool isPressOrDrag = evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag;

            if (isLeftMouse && isPressOrDrag)
            {
                // Enforce spacing
                if (hasPlacedFirst && Vector3.Distance(hit.point, lastPlacedPosition) < brushSpacing)
                {
                    evt.Use();
                    return;
                }

                PlaceEssence(hit.point, hit.normal);
                lastPlacedPosition = hit.point;
                hasPlacedFirst = true;
                evt.Use();
            }

            // Consume right-click to prevent context menu while painting
            if (evt.button == 1 && isPressOrDrag)
                evt.Use();
        }

        private void PlaceEssence(Vector3 position, Vector3 surfaceNormal)
        {
            // Find or create the essences parent
            var parentName = "--- Essences ---";
            var parentGo = GameObject.Find(parentName);
            if (parentGo == null)
            {
                parentGo = new GameObject(parentName);
                Undo.RegisterCreatedObjectUndo(parentGo, "Create Essences Parent");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(essencePrefab);
            if (instance == null) return;

            instance.transform.position = position;
            instance.transform.SetParent(parentGo.transform);

            // Assign the essence data
            var spiritEssence = instance.GetComponent<SpiritEssence>();
            if (spiritEssence != null)
            {
                var so = new SerializedObject(spiritEssence);
                so.FindProperty("essenceData").objectReferenceValue = selectedEssence;
                // Always assign a fresh GUID — instances would otherwise inherit the prefab's
                // shared uniqueId, causing all copies to be marked collected when one is picked up.
                so.FindProperty("uniqueId").stringValue = System.Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
            }

            Undo.RegisterCreatedObjectUndo(instance, "Paint Essence");
        }
    }
}
