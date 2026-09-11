using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CultivationGame.Player;

namespace CultivationGame.Editor
{
    /// <summary>Opt-in, repeatable real-physics input test. Restores the user's save on exit.</summary>
    [InitializeOnLoad]
    public static class CharacterPlaytest
    {
        private const string Guard = "CharacterPlaytest.SaveGuard";
        private const string RunKey = "CharacterPlaytest.Run";
        private static readonly string Backup = Path.GetFullPath("Library/CharacterPlaytest/saveBefore.json");
        private static PlayerMovement player;
        private static float start = -1, lastSample = -1;
        private static StringBuilder rows;
        private static string run;

        static CharacterPlaytest()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Guard, false))
                {
                    string save = Path.Combine(Application.persistentDataPath, "cultivator_save.json");
                    if (File.Exists(Backup)) File.Copy(Backup, save, true);
                    else if (!SessionState.GetBool(Guard + ".existed", false) && File.Exists(save)) File.Delete(save);
                    SessionState.SetBool(Guard, false);
                    SessionState.SetString(RunKey, "");
                }
            };
        }

        public static void Begin(string label)
        {
            if (!SessionState.GetBool(Guard, false))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Backup));
                string save = Path.Combine(Application.persistentDataPath, "cultivator_save.json");
                bool exists = File.Exists(save);
                if (exists) File.Copy(save, Backup, true);
                else if (File.Exists(Backup)) File.Delete(Backup);
                SessionState.SetBool(Guard + ".existed", exists);
                SessionState.SetBool(Guard, true);
            }
            SessionState.SetString(RunKey, label);
            start = -1; rows = null;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.time < 1f) return;
            string requested = SessionState.GetString(RunKey, "");
            if (string.IsNullOrEmpty(requested)) return;
            if (player == null) player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            if (player == null || Keyboard.current == null) return;
            Application.runInBackground = true;
            if (start < 0)
            {
                start = Time.time; lastSample = -1; run = requested;
                rows = new StringBuilder("t,x,y,z,vx,vy,vz,grounded,speed,transition,state,clip\n");
            }
            float t = Time.time - start;
            Key[] keys = t < 1 || t >= 8 ? Array.Empty<Key>() :
                t < 4 ? new[] { Key.W } :
                t >= 6 && t < 6.12f ? new[] { Key.W, Key.LeftShift, Key.Space } :
                new[] { Key.W, Key.LeftShift };
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
            if (t - lastSample >= .019f)
            {
                lastSample = t;
                Vector3 p = player.transform.position, v = player.rb.linearVelocity;
                var animator = player.animator;
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                string clip = clips.Length > 0 ? clips.OrderByDescending(c => c.weight).First().clip.name : "none";
                rows.AppendLine(FormattableString.Invariant($"{t:F4},{p.x:F4},{p.y:F4},{p.z:F4},{v.x:F4},{v.y:F4},{v.z:F4},{player.IsGrounded()},{animator.GetFloat("Speed"):F4},{animator.IsInTransition(0)},{animator.GetCurrentAnimatorStateInfo(0).shortNameHash},{clip}"));
            }
            if (t >= 10)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                Directory.CreateDirectory("ArtSource/Playtests");
                File.WriteAllText("ArtSource/Playtests/" + run + ".csv", rows.ToString());
                SessionState.SetString(RunKey, "");
                start = -1;
                Debug.Log("Character motion test completed: " + run);
            }
        }
    }
}
