using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using CultivationGame.Player;

namespace CultivationGame.Editor
{
    /// <summary>Reproducible import and visual-only installation of the authored character.</summary>
    public static class PrisonAlchemistSetup
    {
        public const string Folder = "Assets/_Project/Characters/PrisonAlchemist";
        public const string ModelPath = Folder + "/PrisonAlchemist.fbx";
        private const string ControllerPath = "Assets/_Project/Animations/PlayerAnimator.controller";

        [MenuItem("Tools/Cultivation/Character/Configure Prison Alchemist Import")]
        public static void ConfigureImport()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var names = new HashSet<string>(transforms.Select(t => t.name));
            var description = importer.humanDescription;
            var mapping = new List<HumanBone>();
            foreach (string humanName in HumanTrait.BoneName)
            {
                if (string.IsNullOrEmpty(humanName)) continue;
                string boneName = humanName.Replace(" ", "");
                if (names.Contains(boneName))
                    mapping.Add(new HumanBone { humanName = humanName, boneName = boneName,
                        limit = new HumanLimit { useDefaultValues = true } });
            }
            description.human = mapping.ToArray();
            description.skeleton = transforms.Select(t => new SkeletonBone {
                name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale
            }).ToArray();
            importer.humanDescription = description;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = clip.takeName.Split('|').Last();
                clip.loopTime = new[] { "Idle", "Walk", "Run", "Fall", "Meditate", "Craft" }.Contains(clip.name);
                clip.loopPose = clip.loopTime;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            if (!AssetDatabase.IsValidFolder(Folder + "/Materials"))
                AssetDatabase.CreateFolder(Folder, "Materials");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable.");
            foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>())
            {
                string materialPath = Folder + "/Materials/" + embedded.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = embedded.name };
                    material.SetColor("_BaseColor", embedded.color);
                    material.SetFloat("_Smoothness", embedded.name == "Copper" ? .55f : .28f);
                    material.SetFloat("_Metallic", embedded.name == "Copper" ? .65f : 0f);
                    // Thin coat panels and lapel strips are intentionally two-sided.
                    material.SetFloat("_Cull", 0);
                    if (embedded.name == "Qi")
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(.025f, .25f, .16f));
                    }
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
            }
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
            if (!avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Invalid humanoid avatar.");
            Debug.Log($"Prison Alchemist import: valid humanoid, {mapping.Count} mapped bones, {clips.Length} clips.");
        }

        [MenuItem("Tools/Cultivation/Character/Install Prison Alchemist Visual")]
        public static void InstallVisual()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            if (player == null) throw new InvalidOperationException("No PlayerMovement in the active scene.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
            foreach (string name in new[] { "Idle", "Walk", "Run", "Jump" })
                if (!clips.ContainsKey(name)) throw new InvalidOperationException("Missing clip: " + name);

            Undo.RecordObject(controller, "Install character animation clips");
            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    var state = child.state;
                    if (state.name == "Movement" && state.motion is BlendTree tree)
                    {
                        Undo.RecordObject(tree, "Match blend thresholds to movement speed");
                        tree.useAutomaticThresholds = false;
                        tree.children = new[] {
                            new ChildMotion { motion = clips["Idle"], threshold = 0, timeScale = 1 },
                            new ChildMotion { motion = clips["Walk"], threshold = player.moveSpeed, timeScale = 1 },
                            new ChildMotion { motion = clips["Run"], threshold = player.sprintSpeed, timeScale = 1 }
                        };
                        EditorUtility.SetDirty(tree);
                    }
                    else if (state.name.IndexOf("Jump", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Undo.RecordObject(state, "Replace jump motion");
                        state.motion = clips["Jump"];
                        EditorUtility.SetDirty(state);
                    }
                }
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var existing = player.transform.Find("PrisonAlchemistVisual");
            var visual = existing != null ? existing.gameObject :
                (GameObject)PrefabUtility.InstantiatePrefab(model, player.transform);
            if (existing == null) Undo.RegisterCreatedObjectUndo(visual, "Install Prison Alchemist");
            visual.name = "PrisonAlchemistVisual";
            visual.transform.localPosition = new Vector3(0, -1, 0);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = Undo.AddComponent<Animator>(visual);
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var oldAnimators = player.GetComponentsInChildren<Animator>(true).Where(a => a != animator).ToArray();
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;
                var serialized = new SerializedObject(behaviour);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue is Animator previous && oldAnimators.Contains(previous))
                        property.objectReferenceValue = animator;
                serialized.ApplyModifiedProperties();
            }
            foreach (var old in oldAnimators)
            {
                Undo.RecordObject(old, "Disable replaced visual animator"); old.enabled = false;
                foreach (var renderer in old.GetComponentsInChildren<Renderer>(true))
                { Undo.RecordObject(renderer, "Hide legacy visual"); renderer.enabled = false; }
            }
            var placeholder = player.GetComponent<MeshRenderer>();
            if (placeholder != null) { Undo.RecordObject(placeholder, "Hide placeholder"); placeholder.enabled = false; }
            Undo.RecordObject(player, "Assign new player animator"); player.animator = animator;
            EditorUtility.SetDirty(controller);
            PrefabUtility.SaveAsPrefabAsset(visual, Folder + "/PrisonAlchemistVisual.prefab");
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = visual;
            Debug.Log("Installed Prison Alchemist visual; player root, physics, scripts, and camera targets preserved.");
        }
    }
}
