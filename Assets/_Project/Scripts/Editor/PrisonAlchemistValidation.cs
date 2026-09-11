using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using CultivationGame.Player;

namespace CultivationGame.Editor
{
    public static class PrisonAlchemistValidation
    {
        [MenuItem("Tools/Cultivation/Character/Validate and Capture Prison Alchemist")]
        public static void Validate()
        {
            var report = new StringBuilder();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            var collider = player.GetComponent<CapsuleCollider>();
            var importer = (ModelImporter)AssetImporter.GetAtPath(PrisonAlchemistSetup.ModelPath);
            report.AppendLine($"Player: {player.name}, world {player.transform.position}, scale {player.transform.localScale}");
            report.AppendLine($"Capsule: height {collider.height}, radius {collider.radius}, center {collider.center}");
            report.AppendLine($"Animator: {player.animator.name}, humanoid {player.animator.isHuman}, root motion {player.animator.applyRootMotion}");
            report.AppendLine($"Visual offset: {player.animator.transform.localPosition}");
            report.AppendLine($"Mapped humanoid bones: {importer.humanDescription.human.Length}");
            foreach (var parameter in player.animator.parameters)
                report.AppendLine($"Parameter: {parameter.name} ({parameter.type})");
            foreach (var clip in player.animator.runtimeAnimatorController.animationClips)
                report.AppendLine($"Clip: {clip.name}, {clip.length:F3}s, loop {clip.isLooping}");
            var importLog = AssetImporter.GetImportLog(PrisonAlchemistSetup.ModelPath);
            if (importLog != null)
                foreach (var entry in importLog.logEntries)
                    report.AppendLine($"Import {entry.flags}: {entry.message}");
            var serializedImporter = new SerializedObject(importer);
            report.AppendLine("Animation import warnings: " + serializedImporter.FindProperty("m_AnimationImportWarnings").arraySize);
            report.AppendLine("Retarget warnings: " + serializedImporter.FindProperty("m_AnimationRetargetingWarnings").arraySize);
            string output = Path.GetFullPath("ArtSource/PrisonAlchemist");
            var preview = new PreviewRenderUtility();
            PlayableGraph graph = default;
            try
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(PrisonAlchemistSetup.ModelPath);
                var instance = UnityEngine.Object.Instantiate(model);
                instance.name = "PrisonAlchemist validation only";
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                preview.AddSingleGO(instance);
                var animator = instance.GetComponent<Animator>();
                animator.runtimeAnimatorController = player.animator.runtimeAnimatorController;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph = PlayableGraph.Create("PrisonAlchemist validation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var controller = AnimatorControllerPlayable.Create(graph, player.animator.runtimeAnimatorController);
                var animationOutput = AnimationPlayableOutput.Create(graph, "Character", animator);
                animationOutput.SetSourcePlayable(controller);
                graph.Play();
                controller.SetLayerWeight(1, 0);
                controller.SetFloat("LocomotionRate", 1);
                controller.SetBool("IsGrounded", true);
                foreach (float speed in new[] { 0f, player.moveSpeed, player.sprintSpeed })
                {
                    controller.SetFloat("Speed", speed);
                    for (int i = 0; i < 45; i++) graph.Evaluate(1f / 60f);
                    report.AppendLine($"Controller speed {speed}: " + string.Join(", ", controller.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name + " weight=" + c.weight.ToString("F2"))));
                    CheckMesh(instance, report);
                    Capture(preview, instance, output + "/Unity_" + (speed == 0 ? "Idle" : speed == player.moveSpeed ? "Walk" : "Run") + ".png");
                }
                controller.SetBool("IsGrounded", false);
                controller.SetFloat("VerticalSpeed", 4);
                controller.SetTrigger("Jump");
                for (int i = 0; i < 30; i++) graph.Evaluate(1f / 60f);
                report.AppendLine("Jump trigger: " + string.Join(", ", controller.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)));
                CheckMesh(instance, report);
                Capture(preview, instance, output + "/Unity_Jump.png");
                controller.SetFloat("VerticalSpeed", -3);
                for (int i = 0; i < 90; i++) graph.Evaluate(1f / 60f);
                if (!controller.GetCurrentAnimatorStateInfo(0).IsName("Fall")) throw new InvalidOperationException("Expected sustained Fall while airborne.");
                report.AppendLine("Variable-airtime check: sustained Fall after 1.5 seconds descending.");
                Capture(preview, instance, output + "/Unity_Fall.png");
                controller.SetBool("IsGrounded", true);
                controller.SetFloat("Speed", 0);
                for (int i = 0; i < 120; i++) graph.Evaluate(1f / 60f);
                report.AppendLine("Grounded recovery: " + string.Join(", ", controller.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)));
                foreach (var action in new[] { "Meditate", "Craft", "Attack", "Hurt", "Death" })
                {
                    controller.SetLayerWeight(1, action == "Craft" || action == "Attack" || action == "Hurt" ? 1 : 0);
                    if (action == "Meditate") controller.SetBool("Meditating", true);
                    else if (action == "Craft") controller.SetBool("Crafting", true);
                    else if (action == "Death") controller.SetBool("Dead", true);
                    else controller.SetTrigger(action);
                    int frames = action == "Death" ? 65 : action == "Attack" || action == "Hurt" ? 8 : 35;
                    for (int i = 0; i < frames; i++) graph.Evaluate(1f / 60f);
                    int layer = action == "Craft" || action == "Attack" || action == "Hurt" ? 1 : 0;
                    report.AppendLine(action + ": " + string.Join(", ", controller.GetCurrentAnimatorClipInfo(layer).Select(c => c.clip.name)));
                    Capture(preview, instance, output + "/Unity_" + action + ".png");
                    controller.SetBool("Meditating", false); controller.SetBool("Crafting", false); controller.SetBool("Dead", false);
                    controller.SetLayerWeight(1, 0);
                    for (int i = 0; i < 60; i++) graph.Evaluate(1f / 60f);
                }
                Capture(preview, instance, output + "/Unity_FarLOD.png", "Far");
                report.AppendLine($"In-place instance position after evaluation: {instance.transform.position}");
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                preview.Cleanup();
                File.WriteAllText(output + "/unity_validation.txt", report.ToString());
            }
            Debug.Log("Prison Alchemist validation saved to ArtSource/PrisonAlchemist/unity_validation.txt");
        }

        private static void CheckMesh(GameObject instance, StringBuilder report)
        {
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh);
                    if (mesh.vertices.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)))
                        throw new InvalidOperationException("Non-finite skinned vertices.");
                    report.AppendLine($"  Skinned mesh: {mesh.vertexCount} vertices; bounds {mesh.bounds}; {renderer.sharedMaterials.Length} materials");
                }
                finally { UnityEngine.Object.DestroyImmediate(mesh); }
            }
        }

        private static void Capture(PreviewRenderUtility preview, GameObject instance, string path, string detail = "Body")
        {
            // Manual graph evaluation can reuse Unity's GPU skinning buffer within
            // one Editor frame. Bake this evaluated pose for an accurate still.
            var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bakedObjects = new System.Collections.Generic.List<GameObject>();
            var bakedMeshes = new System.Collections.Generic.List<Mesh>();
            foreach (var skin in skins)
            {
                skin.enabled = false;
                if (!skin.name.Contains(detail)) continue;
                var baked = new Mesh();
                skin.BakeMesh(baked);
                var go = new GameObject("Evaluated pose snapshot");
                go.transform.SetParent(skin.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = baked;
                go.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                skin.enabled = false;
                bakedObjects.Add(go); bakedMeshes.Add(baked);
            }
            try
            {
            preview.camera.transform.position = new Vector3(3.1f, 2.4f, 5.7f);
            preview.camera.transform.LookAt(new Vector3(0, 1.02f, 0));
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 1.18f;
            preview.camera.nearClipPlane = .01f;
            preview.camera.farClipPlane = 30;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.06f, .08f, .10f);
            preview.lights[0].intensity = 1.5f;
            preview.lights[0].transform.rotation = Quaternion.Euler(40, 210, 0);
            preview.lights[1].intensity = 1f;
            preview.ambientColor = new Color(.45f, .45f, .45f);
            preview.BeginStaticPreview(new Rect(0, 0, 800, 1000));
            preview.Render(true);
            var texture = preview.EndStaticPreview();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                foreach (var go in bakedObjects) UnityEngine.Object.DestroyImmediate(go);
                foreach (var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                foreach (var skin in skins) skin.enabled = true;
            }
        }
    }
}
