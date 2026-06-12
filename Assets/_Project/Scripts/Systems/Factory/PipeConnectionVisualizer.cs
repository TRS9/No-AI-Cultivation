using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Visualizes existing SpiritPipe connections using LineRenderers.
    /// Automatically creates and manages connection lines when pipes are
    /// connected or disconnected. Attach to any persistent GameObject in the scene
    /// (e.g. a Managers root).
    /// </summary>
    public class PipeConnectionVisualizer : MonoBehaviour
    {
        [Header("Line Appearance")]
        [SerializeField] [Tooltip("Color of the connection lines drawn between connected machines.")] private Color lineColor = new Color(0.3f, 0.7f, 1f, 0.8f);
        [SerializeField] [Tooltip("World-space width of the pipe connection visualization lines.")] private float lineWidth = 0.06f;
        [SerializeField] [Tooltip("Vertical offset applied to both line endpoints to raise lines above machines.")] private float heightOffset = 0.5f;

        private readonly Dictionary<SpiritPipe, LineRenderer> _lines = new();
        private readonly List<SpiritPipe> _toRemove = new();

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        private void OnEnable()
        {
            GameDataEvents.OnPipeConnected += OnPipeConnected;
            GameDataEvents.OnPipeDisconnected += OnPipeDisconnected;
        }

        private void OnDisable()
        {
            GameDataEvents.OnPipeConnected -= OnPipeConnected;
            GameDataEvents.OnPipeDisconnected -= OnPipeDisconnected;
        }

        private void Start()
        {
            // Pick up pipes that were already connected before this component enabled
            // (e.g. loaded from a save file).
            foreach (var pipe in FindObjectsByType<SpiritPipe>(FindObjectsSortMode.None))
            {
                if (!pipe.IsConnected) continue;

                var src = pipe.Source as MonoBehaviour;
                var dst = pipe.Destination as MonoBehaviour;

                if (src != null && dst != null)
                    CreateLine(pipe, src.transform, dst.transform);
            }
        }

        private void Update()
        {
            // Machines never move once placed, so line positions are set once in
            // CreateLine — this loop only removes lines whose pipe or endpoints died.
            _toRemove.Clear();

            foreach (var kv in _lines)
            {
                var pipe = kv.Key;
                var lr = kv.Value;

                bool endpointDestroyed = pipe != null && pipe.IsConnected &&
                    ((pipe.Source as MonoBehaviour) == null ||
                     (pipe.Destination as MonoBehaviour) == null);

                if (pipe == null || lr == null || !pipe.IsConnected || endpointDestroyed)
                {
                    if (lr != null)
                    {
                        if (lr.material != null) Destroy(lr.material);
                        Destroy(lr.gameObject);
                    }
                    _toRemove.Add(pipe);
                }
            }

            foreach (var key in _toRemove)
                _lines.Remove(key);
        }

        // ------------------------------------------------------------------ //
        //  Event callbacks
        // ------------------------------------------------------------------ //

        private void OnPipeConnected(MonoBehaviour pipe, MonoBehaviour source, MonoBehaviour destination)
        {
            if (pipe is not SpiritPipe spiritPipe) return;
            if (_lines.ContainsKey(spiritPipe)) return;

            CreateLine(spiritPipe, source.transform, destination.transform);
        }

        private void OnPipeDisconnected(MonoBehaviour pipe)
        {
            if (pipe is not SpiritPipe spiritPipe) return;
            RemoveLine(spiritPipe);
        }

        // ------------------------------------------------------------------ //
        //  Line management
        // ------------------------------------------------------------------ //

        private void CreateLine(SpiritPipe pipe, Transform source, Transform destination)
        {
            var go = new GameObject("PipeConnectionLine");
            go.transform.SetParent(pipe.transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var mat = CreateDefaultLineMaterial();
            if (mat != null)
            {
                lr.material = mat;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
            }

            SetLinePositions(lr, source, destination);
            _lines[pipe] = lr;
        }

        private void RemoveLine(SpiritPipe pipe)
        {
            if (!_lines.TryGetValue(pipe, out var lr)) return;

            if (lr != null)
            {
                // The material was created per-line in CreateLine and is not
                // destroyed with the GameObject — release it explicitly.
                if (lr.material != null) Destroy(lr.material);
                Destroy(lr.gameObject);
            }
            _lines.Remove(pipe);
        }

        private void SetLinePositions(LineRenderer lr, Transform source, Transform destination)
        {
            var offset = Vector3.up * heightOffset;
            lr.SetPosition(0, source.position + offset);
            lr.SetPosition(1, destination.position + offset);
        }

        private Material CreateDefaultLineMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            if (shader == null) return null;

            var mat = new Material(shader);
            mat.color = lineColor;
            return mat;
        }
    }
}
