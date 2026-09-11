using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using CultivationGame.Core;
using CultivationGame.Player;

namespace CultivationGame.Editor
{
    public static class CharacterGameplaySetup
    {
        [Serializable] private class Swatch { public string name; public float r, g, b; }
        [Serializable] private class Palette { public Swatch[] swatches; }
        private const string Folder = PrisonAlchemistSetup.Folder;

        [MenuItem("Tools/Cultivation/Character/Apply Gameplay Polish")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            PrisonAlchemistSetup.ConfigureImport();
            ConfigurePalette();
            ConfigureBuildingPreview();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            Undo.RecordObject(player, "Tune player locomotion");
            player.moveSpeed = 1.8f; player.sprintSpeed = 4.8f;
            player.acceleration = 22; player.braking = 45; player.airAcceleration = 7;
            player.jumpForce = 6; player.fallGravityMultiplier = 2;
            player.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var health = player.GetComponent<HealthSystem>() ?? Undo.AddComponent<HealthSystem>(player.gameObject);
            var combat = player.GetComponent<PlayerCombatController>() ?? Undo.AddComponent<PlayerCombatController>(player.gameObject);
            combat.healthSystem = health; combat.playerStats = player.GetComponent<PlayerStats>();
            combat.dodgeDistance = 2.8f; combat.dodgeDuration = .33f; combat.enemyLayer = ~0;
            var input = player.GetComponent<PlayerInput>();
            combat.attackAction = ActionReference(input.actions.FindAction("Player/Attack"), "Attack");
            combat.dodgeAction = ActionReference(input.actions.FindAction("Player/Crouch"), "Dodge");
            PrisonAlchemistSetup.InstallVisual();
            var visual = player.animator.gameObject;
            combat.animator = player.animator;
            if (visual.GetComponent<PlayerAnimationDriver>() == null) Undo.AddComponent<PlayerAnimationDriver>(visual);
            ConfigureController((AnimatorController)player.animator.runtimeAnimatorController, player);
            var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var lod = visual.GetComponent<LODGroup>();
            if (lod == null) lod = Undo.AddComponent<LODGroup>(visual);
            foreach (var skin in skins) { skin.gameObject.SetActive(true); skin.enabled = true; }
            lod.SetLODs(new[] {
                new LOD(.25f, skins.Where(s => s.name.Contains("Body")).Cast<Renderer>().ToArray()),
                new LOD(.10f, skins.Where(s => s.name.Contains("Medium")).Cast<Renderer>().ToArray()),
                new LOD(.015f, skins.Where(s => s.name.Contains("Far")).Cast<Renderer>().ToArray())
            });
            lod.RecalculateBounds();
            var palette = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/AlchemistPalette.mat");
            foreach (var skin in skins) skin.sharedMaterial = palette;
            // A readable lower silhouette without clearing the terrain's detail map.
            foreach (var terrain in Terrain.activeTerrains)
            {
                var data = terrain.terrainData; var details = data.detailPrototypes;
                Undo.RecordObject(data, "Lower grass for character visibility");
                foreach (var detail in details)
                    if (!detail.usePrototypeMesh || (detail.prototype != null && detail.prototype.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0))
                    { detail.minHeight = Mathf.Min(detail.minHeight, .22f); detail.maxHeight = Mathf.Min(detail.maxHeight, .45f); }
                data.detailPrototypes = details; EditorUtility.SetDirty(data);
                Undo.RecordObject(terrain, "Reduce grass draw density");
                terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity, .65f);
            }
            EditorUtility.SetDirty(player); EditorUtility.SetDirty(combat);
            PrefabUtility.SaveAsPrefabAsset(visual, Folder + "/PrisonAlchemistVisual.prefab");
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Gameplay polish applied: phased jumping, gameplay actions, palette atlas, three LODs and shorter grass.");
        }

        private static InputActionReference ActionReference(InputAction action, string name)
        {
            string path = Folder + "/" + name + "Input.asset";
            var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (reference == null) { reference = InputActionReference.Create(action); AssetDatabase.CreateAsset(reference, path); }
            else reference.Set(action);
            reference.name = name + "Input";
            EditorUtility.SetDirty(reference);
            return reference;
        }

        private static void ConfigureBuildingPreview()
        {
            var placement = UnityEngine.Object.FindFirstObjectByType<CultivationGame.Systems.PlacementController>();
            if (placement == null) return;
            var settings = new SerializedObject(placement);
            foreach (bool valid in new[] { true, false })
            {
                string path = Folder + "/Materials/BuildPreview" + (valid ? "Valid" : "Invalid") + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", valid ? new Color(.08f, .85f, .45f, .5f) : new Color(1, .12f, .08f, .5f));
                material.SetFloat("_Surface", 1);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                EditorUtility.SetDirty(material);
                settings.FindProperty(valid ? "ghostValidMaterial" : "ghostInvalidMaterial").objectReferenceValue = material;
            }
            settings.ApplyModifiedProperties();
        }

        private static void ConfigurePalette()
        {
            var palette = JsonUtility.FromJson<Palette>(File.ReadAllText(Folder + "/palette_swatches.json"));
            var mask = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                int index = (y / 16) * 4 + x / 16;
                bool copper = index < palette.swatches.Length && palette.swatches[index].name == "Copper";
                mask.SetPixel(x, y, new Color(copper ? .65f : 0, 0, 0, copper ? .55f : .28f));
            }
            mask.Apply(); File.WriteAllBytes(Folder + "/AlchemistMetallic.png", mask.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(Folder + "/AlchemistMetallic.png");
            foreach (string texture in new[] { "AlchemistPalette.png", "AlchemistMetallic.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + texture);
                importer.sRGBTexture = false; importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/AlchemistPalette.mat");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/AlchemistPalette.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/AlchemistMetallic.png"));
            material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness", 1);
            material.SetFloat("_Cull", 0); EditorUtility.SetDirty(material);
        }

        private static void ConfigureController(AnimatorController controller, PlayerMovement player)
        {
            void Parameter(string name, AnimatorControllerParameterType type, float initial = 0)
            {
                if (!controller.parameters.Any(p => p.name == name))
                    controller.AddParameter(new AnimatorControllerParameter { name = name, type = type, defaultFloat = initial });
            }
            Parameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            Parameter("LocomotionRate", AnimatorControllerParameterType.Float, 1);
            foreach (string name in new[] { "Meditating", "Crafting", "Dead" }) Parameter(name, AnimatorControllerParameterType.Bool);
            foreach (string name in new[] { "Attack", "Dodge", "Hurt" }) Parameter(name, AnimatorControllerParameterType.Trigger);
            var clips = AssetDatabase.LoadAllAssetsAtPath(PrisonAlchemistSetup.ModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
            var sm = controller.layers[0].stateMachine;
            var oldJump = sm.states.FirstOrDefault(s => s.state.name.Contains("Jump")).state;
            if (oldJump != null) oldJump.name = "Jump";
            AnimatorState State(AnimatorStateMachine machine, string name)
            {
                var state = machine.states.FirstOrDefault(s => s.state.name == name).state ?? machine.AddState(name);
                if (clips.TryGetValue(name, out var clip)) state.motion = clip;
                state.writeDefaultValues = false;
                foreach (var transition in state.transitions) state.RemoveTransition(transition);
                return state;
            }
            AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, string condition = null,
                AnimatorConditionMode mode = AnimatorConditionMode.If, float threshold = 0, bool exit = false, float duration = .06f)
            {
                var transition = from.AddTransition(to); transition.hasExitTime = exit; transition.exitTime = .82f;
                transition.hasFixedDuration = true; transition.duration = duration; transition.canTransitionToSelf = false;
                if (condition != null) transition.AddCondition(mode, threshold, condition);
                return transition;
            }
            var movement = State(sm, "Movement");
            movement.speedParameter = "LocomotionRate"; movement.speedParameterActive = true; movement.iKOnFeet = true;
            var tree = (BlendTree)movement.motion; tree.useAutomaticThresholds = false;
            tree.children = new[] {
                new ChildMotion { motion = clips["Idle"], threshold = 0, timeScale = 1, directBlendParameter = "Speed" },
                new ChildMotion { motion = clips["Walk"], threshold = player.moveSpeed, timeScale = player.moveSpeed / 1.5833333f, directBlendParameter = "Speed" },
                new ChildMotion { motion = clips["Run"], threshold = player.sprintSpeed, timeScale = player.sprintSpeed / 3.525f, directBlendParameter = "Speed" }
            };
            var jump = State(sm, "Jump"); var fall = State(sm, "Fall"); var land = State(sm, "Land");
            var meditate = State(sm, "Meditate"); var death = State(sm, "Death"); var dodge = State(sm, "Dodge");
            foreach (var transition in sm.anyStateTransitions) sm.RemoveAnyStateTransition(transition);
            void Any(AnimatorState target, string condition, bool deadGuard = true)
            {
                var transition = sm.AddAnyStateTransition(target); transition.hasExitTime = false;
                transition.hasFixedDuration = true; transition.duration = .05f; transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.If, 0, condition);
                if (deadGuard) transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            }
            Any(death, "Dead", false); Any(meditate, "Meditating"); Any(jump, "Jump"); Any(dodge, "Dodge");
            Transition(movement, fall, "IsGrounded", AnimatorConditionMode.IfNot);
            Transition(jump, fall, "VerticalSpeed", AnimatorConditionMode.Less, 0);
            Transition(jump, land, "IsGrounded"); Transition(fall, land, "IsGrounded");
            Transition(land, movement, "Speed", AnimatorConditionMode.Greater, .15f, duration: .045f);
            Transition(land, movement, exit: true); Transition(land, fall, "IsGrounded", AnimatorConditionMode.IfNot);
            Transition(meditate, movement, "Meditating", AnimatorConditionMode.IfNot, duration: .15f);
            Transition(death, movement, "Dead", AnimatorConditionMode.IfNot);
            Transition(dodge, movement, exit: true); sm.defaultState = movement;
            var layers = controller.layers; layers[0].iKPass = true; controller.layers = layers;
            int upperIndex = Array.FindIndex(controller.layers, layer => layer.name == "Upper Body");
            if (upperIndex < 0) { controller.AddLayer("Upper Body"); upperIndex = controller.layers.Length - 1; }
            layers = controller.layers;
            string maskPath = Folder + "/UpperBody.mask";
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (mask == null) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, maskPath); }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers }) mask.SetHumanoidBodyPartActive(part, true);
            layers[upperIndex].avatarMask = mask; layers[upperIndex].defaultWeight = 0; controller.layers = layers;
            var upper = layers[upperIndex].stateMachine; var empty = State(upper, "Empty"); upper.defaultState = empty;
            foreach (var transition in upper.anyStateTransitions) upper.RemoveAnyStateTransition(transition);
            foreach (string name in new[] { "Attack", "Hurt", "Craft" })
            {
                var state = State(upper, name); var enter = upper.AddAnyStateTransition(state);
                enter.hasExitTime = false; enter.duration = .045f; enter.hasFixedDuration = true; enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0, name == "Craft" ? "Crafting" : name);
                enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead"); enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Meditating");
                if (name == "Craft") Transition(state, empty, "Crafting", AnimatorConditionMode.IfNot);
                else Transition(state, empty, exit: true);
                Transition(state, empty, "Dead"); Transition(state, empty, "Meditating");
            }
            EditorUtility.SetDirty(mask); EditorUtility.SetDirty(tree); EditorUtility.SetDirty(controller);
        }
    }
}
