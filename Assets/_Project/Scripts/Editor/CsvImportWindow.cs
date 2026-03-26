using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Editor window that reads a sectioned CSV file and generates ScriptableObject assets.
    /// Open via <b>Tools → CSV ScriptableObject Importer</b>.
    ///
    /// <para><b>CSV Format</b></para>
    /// <code>
    /// #Type,RawMaterialData
    /// _assetName,description,qiValue,itemType,materialName,materialColor
    /// IronOre,A common mineral,10,RawMaterial,Iron,#808080
    ///
    /// #Type,RecipeData
    /// _assetName,recipeName,description,craftingDuration,requiredMachine,successRate,qiCost,inputs,outputs
    /// SmeltIron,Iron Smelting,Smelt iron ore,5,Furnace,0.9,10,IronOre:2,IronIngot:1
    /// </code>
    ///
    /// <para><b>Rules</b></para>
    /// <list type="bullet">
    ///   <item><c>#Type,TypeName</c> starts a new section.</item>
    ///   <item>Next line = column headers (must match C# field names).</item>
    ///   <item>Following lines = data rows.</item>
    ///   <item><c>_assetName</c> column is required (used as .asset file name).</item>
    ///   <item><c>_assetPath</c> column is optional (subfolder inside the output folder).</item>
    ///   <item>Empty lines and lines starting with <c>#</c> (except <c>#Type</c>) are ignored.</item>
    /// </list>
    ///
    /// <para><b>Supported value formats</b></para>
    /// <list type="bullet">
    ///   <item>Color: <c>#RRGGBB</c> or <c>#RRGGBBAA</c></item>
    ///   <item>Vector2Int: <c>x;y</c></item>
    ///   <item>Vector2 / Vector3: <c>x;y</c> / <c>x;y;z</c></item>
    ///   <item>RecipeIngredient list: <c>ItemName:Amount|ItemName:Amount</c></item>
    ///   <item>LootDrop array: <c>ItemName:min:max:chance|…</c></item>
    ///   <item>ScriptableObject / Sprite / GameObject references: asset name</item>
    /// </list>
    /// </summary>
    public class CsvImportWindow : EditorWindow
    {
        // ─── UI State ─────────────────────────────────────────────────────
        private string _csvPath = "";
        private string _outputFolder = "Assets/_Project/Data/Generated";
        private Vector2 _previewScroll;
        private Vector2 _logScroll;
        private readonly List<string> _log = new();
        private List<CsvSection> _sections = new();
        private bool _parsed;
        private bool _overwriteExisting;

        // Assets created during current generation run (for cross-reference resolution).
        private readonly Dictionary<string, ScriptableObject> _createdAssets =
            new(StringComparer.OrdinalIgnoreCase);

        // Cache for asset lookups to avoid repeated AssetDatabase queries.
        private readonly Dictionary<(string name, Type type), UnityEngine.Object> _assetLookupCache =
            new();

        // ─── Data Structures ──────────────────────────────────────────────
        private class CsvSection
        {
            public string TypeName;
            public string[] Headers;
            public readonly List<Dictionary<string, string>> Rows = new();
        }

        // ─── Menu & Lifecycle ─────────────────────────────────────────────
        [MenuItem("Tools/CSV ScriptableObject Importer")]
        public static void Open() => GetWindow<CsvImportWindow>("CSV SO Importer");

        // ─── GUI ──────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Label("CSV ScriptableObject Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawFileSelection();
            DrawOptions();
            EditorGUILayout.Space(4);
            DrawActionButtons();
            EditorGUILayout.Space(4);
            DrawPreview();
            DrawLog();
        }

        private void DrawFileSelection()
        {
            // CSV file
            EditorGUILayout.BeginHorizontal();
            _csvPath = EditorGUILayout.TextField("CSV File", _csvPath);
            if (GUILayout.Button("Browse\u2026", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select CSV File", Application.dataPath, "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _csvPath = path;
                    _parsed = false;
                    _sections.Clear();
                    _log.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Output folder
            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("Browse\u2026", GUILayout.Width(80)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Output Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    if (folder.StartsWith(Application.dataPath, StringComparison.Ordinal))
                        folder = "Assets" + folder.Substring(Application.dataPath.Length);
                    _outputFolder = folder;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptions()
        {
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", _overwriteExisting);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Parse
            bool canParse = !string.IsNullOrEmpty(_csvPath) && File.Exists(_csvPath);
            GUI.enabled = canParse;
            if (GUILayout.Button("Parse CSV", GUILayout.Height(28)))
            {
                _log.Clear();
                _sections = ParseCsv(_csvPath);
                _parsed = _sections.Count > 0;
                Repaint();
            }

            // Generate
            GUI.enabled = _parsed && _sections.Count > 0;
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Generate Assets", GUILayout.Height(28)))
            {
                GenerateAssets();
                Repaint();
            }
            GUI.backgroundColor = prevBg;
            GUI.enabled = true;

            // Export template
            if (GUILayout.Button("Export Template CSV", GUILayout.Height(28)))
            {
                ExportTemplateCsv();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            if (!_parsed || _sections.Count == 0) return;

            GUILayout.Label(
                $"Preview \u2014 {_sections.Count} section(s)",
                EditorStyles.boldLabel);
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MaxHeight(180));
            foreach (CsvSection section in _sections)
            {
                EditorGUILayout.LabelField(
                    $"\u25B8 {section.TypeName}  ({section.Rows.Count} row(s))",
                    EditorStyles.miniBoldLabel);
                foreach (Dictionary<string, string> row in section.Rows)
                {
                    string assetName = row.TryGetValue("_assetName", out string n) ? n : "(no _assetName)";
                    EditorGUILayout.LabelField($"     \u2022 {assetName}");
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawLog()
        {
            if (_log.Count == 0) return;

            EditorGUILayout.Space(4);
            GUILayout.Label("Log", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MaxHeight(200));
            foreach (string entry in _log)
                EditorGUILayout.LabelField(entry, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        // ─── CSV Parsing ──────────────────────────────────────────────────

        private List<CsvSection> ParseCsv(string path)
        {
            var sections = new List<CsvSection>();
            CsvSection current = null;

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Skip empty lines and pure comments
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)
                    && !line.StartsWith("#Type", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Section header: #Type,TypeName
                if (line.StartsWith("#Type", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = ParseCsvLine(line);
                    if (parts.Length < 2)
                    {
                        Log($"Line {i + 1}: Invalid #Type header \u2014 expected '#Type,TypeName'");
                        continue;
                    }
                    current = new CsvSection { TypeName = parts[1].Trim() };
                    sections.Add(current);
                    Log($"Found section: {current.TypeName}");
                    continue;
                }

                if (current == null)
                {
                    Log($"Line {i + 1}: Data outside of a #Type section \u2014 skipped");
                    continue;
                }

                // Column headers (first data line after #Type)
                if (current.Headers == null)
                {
                    current.Headers = ParseCsvLine(line).Select(h => h.Trim()).ToArray();
                    continue;
                }

                // Data row
                string[] values = ParseCsvLine(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < current.Headers.Length && c < values.Length; c++)
                    row[current.Headers[c]] = values[c].Trim();
                current.Rows.Add(row);
            }

            Log($"Parsed {sections.Count} section(s), {sections.Sum(s => s.Rows.Count)} total row(s).");
            return sections;
        }

        /// <summary>Parses a single CSV line, respecting double-quoted fields.</summary>
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            fields.Add(sb.ToString());
            return fields.ToArray();
        }

        // ─── Asset Generation ─────────────────────────────────────────────

        private void GenerateAssets()
        {
            int created = 0, skipped = 0, errors = 0;
            _createdAssets.Clear();
            _assetLookupCache.Clear();

            foreach (CsvSection section in _sections)
            {
                Type type = ResolveSOType(section.TypeName);
                if (type == null)
                {
                    Log($"\u2718 Unknown type '{section.TypeName}' \u2014 skipping section.");
                    errors += section.Rows.Count;
                    continue;
                }

                foreach (Dictionary<string, string> row in section.Rows)
                {
                    if (!row.TryGetValue("_assetName", out string assetName)
                        || string.IsNullOrEmpty(assetName))
                    {
                        Log($"  \u2718 Row missing '_assetName' in {section.TypeName} \u2014 skipped.");
                        errors++;
                        continue;
                    }

                    // Determine target folder
                    string subFolder = row.TryGetValue("_assetPath", out string sf)
                        && !string.IsNullOrEmpty(sf) ? sf : "";
                    string folder = string.IsNullOrEmpty(subFolder)
                        ? _outputFolder
                        : $"{_outputFolder}/{subFolder}";

                    EnsureFolder(folder);
                    string assetPath = $"{folder}/{SanitizeFileName(assetName)}.asset";

                    // Check existing
                    var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (existing != null && !_overwriteExisting)
                    {
                        Log($"  \u23ED '{assetName}' already exists \u2014 skipped.");
                        if (type.IsInstanceOfType(existing))
                            _createdAssets.TryAdd(assetName, existing);
                        skipped++;
                        continue;
                    }

                    // Create or reuse
                    ScriptableObject so = existing != null && existing.GetType() == type
                        ? existing
                        : ScriptableObject.CreateInstance(type);

                    // Set fields
                    foreach (KeyValuePair<string, string> kvp in row)
                    {
                        if (kvp.Key.StartsWith("_", StringComparison.Ordinal)) continue;
                        if (string.IsNullOrEmpty(kvp.Value)) continue;
                        SetField(so, kvp.Key, kvp.Value);
                    }

                    // Save
                    if (existing == null || existing.GetType() != type)
                        AssetDatabase.CreateAsset(so, assetPath);
                    else
                        EditorUtility.SetDirty(so);

                    _createdAssets[assetName] = so;
                    Log($"  \u2714 {section.TypeName}: {assetName}");
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Log($"\nDone \u2014 Created: {created}, Skipped: {skipped}, Errors: {errors}");
        }

        // ─── Type Resolution ──────────────────────────────────────────────

        private static Dictionary<string, Type> s_typeCache;

        private static Type ResolveSOType(string typeName)
        {
            if (s_typeCache == null)
            {
                s_typeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type t in asm.GetTypes())
                        {
                            if (!t.IsAbstract && t.IsSubclassOf(typeof(ScriptableObject)))
                            {
                                s_typeCache.TryAdd(t.Name, t);
                                if (t.FullName != null)
                                    s_typeCache.TryAdd(t.FullName, t);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip assemblies whose types cannot be loaded.
                    }
                }
            }

            return s_typeCache.TryGetValue(typeName, out Type result) ? result : null;
        }

        // ─── Field Setting ────────────────────────────────────────────────

        private void SetField(ScriptableObject so, string fieldName, string value)
        {
            FieldInfo field = FindField(so.GetType(), fieldName);
            if (field == null)
            {
                Log($"    \u26A0 Field '{fieldName}' not found on {so.GetType().Name}");
                return;
            }

            try
            {
                object converted = ConvertValue(value, field.FieldType);
                field.SetValue(so, converted);
            }
            catch (Exception ex)
            {
                Log($"    \u26A0 '{fieldName}' = '{value}': {ex.Message}");
            }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            Type current = type;
            while (current != null && current != typeof(UnityEngine.Object))
            {
                FieldInfo field = current.GetField(
                    name,
                    BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    if (field.IsPublic
                        || Attribute.IsDefined(field, typeof(SerializeField)))
                        return field;
                }
                current = current.BaseType;
            }
            return null;
        }

        // ─── Value Conversion ─────────────────────────────────────────────

        private object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            // Nullable<T>
            Type underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
                return ConvertValue(value, underlying);

            // ── Primitives ──
            if (targetType == typeof(string))  return value;
            if (targetType == typeof(int))     return int.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(float))   return float.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double))  return double.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))    return bool.Parse(value);
            if (targetType == typeof(long))    return long.Parse(value, CultureInfo.InvariantCulture);

            // ── Enum ──
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, ignoreCase: true);

            // ── Color (#RRGGBB / #RRGGBBAA) ──
            if (targetType == typeof(Color))
            {
                string col = value.StartsWith("#", StringComparison.Ordinal) ? value : "#" + value;
                return ColorUtility.TryParseHtmlString(col, out Color color) ? color : Color.white;
            }

            // ── Vectors (semicolon-separated to avoid CSV conflicts) ──
            if (targetType == typeof(Vector2Int))
            {
                string[] parts = value.Split(';');
                return parts.Length >= 2
                    ? new Vector2Int(
                        int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture))
                    : Vector2Int.zero;
            }
            if (targetType == typeof(Vector2))
            {
                string[] parts = value.Split(';');
                return parts.Length >= 2
                    ? new Vector2(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture))
                    : Vector2.zero;
            }
            if (targetType == typeof(Vector3))
            {
                string[] parts = value.Split(';');
                return parts.Length >= 3
                    ? new Vector3(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture))
                    : Vector3.zero;
            }

            // ── RecipeIngredient collections ──
            if (targetType == typeof(List<RecipeIngredient>))
                return ParseRecipeIngredients(value);
            if (targetType == typeof(RecipeIngredient[]))
                return ParseRecipeIngredients(value).ToArray();

            // ── LootDrop array ──
            if (targetType == typeof(LootDrop[]))
                return ParseLootDrops(value);

            // ── Generic List<T> of ScriptableObjects ──
            if (targetType.IsGenericType
                && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elemType = targetType.GetGenericArguments()[0];
                if (typeof(ScriptableObject).IsAssignableFrom(elemType))
                    return ParseSOList(value, targetType, elemType);
            }

            // ── Array of ScriptableObjects ──
            if (targetType.IsArray)
            {
                Type elemType = targetType.GetElementType();
                if (elemType != null && typeof(ScriptableObject).IsAssignableFrom(elemType))
                    return ParseSOArray(value, elemType);
            }

            // ── Single ScriptableObject reference ──
            if (typeof(ScriptableObject).IsAssignableFrom(targetType))
                return FindAssetByName(value, targetType);

            // ── Sprite ──
            if (targetType == typeof(Sprite))
                return FindAssetByName(value, typeof(Sprite));

            // ── GameObject ──
            if (targetType == typeof(GameObject))
                return FindAssetByName(value, typeof(GameObject));

            Log($"    \u26A0 Unsupported field type: {targetType.Name}");
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // ─── Special Type Parsers ─────────────────────────────────────────

        /// <summary>Parses <c>ItemName:Amount|ItemName:Amount</c> into a list.</summary>
        private List<RecipeIngredient> ParseRecipeIngredients(string value)
        {
            var list = new List<RecipeIngredient>();
            foreach (string entry in value.Split('|'))
            {
                string[] parts = entry.Trim().Split(':');
                if (parts.Length < 2) continue;
                var item = FindAssetByName(parts[0].Trim(), typeof(ItemData)) as ItemData;
                if (item == null)
                {
                    Log($"    \u26A0 RecipeIngredient: Item '{parts[0].Trim()}' not found");
                    continue;
                }
                list.Add(new RecipeIngredient
                {
                    item = item,
                    amount = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture)
                });
            }
            return list;
        }

        /// <summary>Parses <c>ItemName:min:max:chance|…</c> into an array.</summary>
        private LootDrop[] ParseLootDrops(string value)
        {
            var list = new List<LootDrop>();
            foreach (string entry in value.Split('|'))
            {
                string[] parts = entry.Trim().Split(':');
                if (parts.Length < 4) continue;
                var item = FindAssetByName(parts[0].Trim(), typeof(ItemData)) as ItemData;
                if (item == null)
                {
                    Log($"    \u26A0 LootDrop: Item '{parts[0].Trim()}' not found");
                    continue;
                }
                list.Add(new LootDrop
                {
                    item = item,
                    minAmount = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    maxAmount = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                    dropChance = float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture)
                });
            }
            return list.ToArray();
        }

        /// <summary>Parses pipe-separated asset names into a <c>List&lt;T&gt;</c>.</summary>
        private object ParseSOList(string value, Type listType, Type elemType)
        {
            IList list = (IList)Activator.CreateInstance(listType);
            foreach (string entry in value.Split('|'))
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                UnityEngine.Object asset = FindAssetByName(trimmed, elemType);
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        /// <summary>Parses pipe-separated asset names into a typed array.</summary>
        private object ParseSOArray(string value, Type elemType)
        {
            var items = new List<UnityEngine.Object>();
            foreach (string entry in value.Split('|'))
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                UnityEngine.Object asset = FindAssetByName(trimmed, elemType);
                if (asset != null) items.Add(asset);
            }
            Array arr = Array.CreateInstance(elemType, items.Count);
            for (int i = 0; i < items.Count; i++)
                arr.SetValue(items[i], i);
            return arr;
        }

        // ─── Asset Lookup ─────────────────────────────────────────────────

        /// <summary>
        /// Finds an asset by name and type. Checks the local cache of assets
        /// created during this generation run first, then falls back to
        /// <see cref="AssetDatabase"/>.
        /// </summary>
        private UnityEngine.Object FindAssetByName(string name, Type type)
        {
            // Check recently created assets first (handles cross-references within one import)
            if (_createdAssets.TryGetValue(name, out ScriptableObject created)
                && type.IsInstanceOfType(created))
                return created;

            // Check lookup cache to avoid repeated AssetDatabase queries
            var cacheKey = (name, type);
            if (_assetLookupCache.TryGetValue(cacheKey, out UnityEngine.Object cached))
                return cached;

            // Search in the AssetDatabase
            UnityEngine.Object found = SearchAssetDatabase(name, type);
            if (found == null)
                Log($"    \u26A0 Asset '{name}' of type {type.Name} not found in project.");

            _assetLookupCache[cacheKey] = found;
            return found;
        }

        private static UnityEngine.Object SearchAssetDatabase(string name, Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:{type.Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset != null
                    && asset.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return asset;
            }

            // Broader search for base-type references (e.g. ItemData when looking for a PillData)
            if (type != typeof(ScriptableObject)
                && typeof(ScriptableObject).IsAssignableFrom(type))
            {
                guids = AssetDatabase.FindAssets($"{name} t:ScriptableObject");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                    if (asset != null
                        && asset.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return asset;
                }
            }

            return null;
        }

        // ─── Template Export ──────────────────────────────────────────────

        /// <summary>
        /// Exports a CSV template file with sections for every known concrete
        /// ScriptableObject type in the Data assembly. Column headers are
        /// derived from the serialized fields of each type.
        /// </summary>
        private void ExportTemplateCsv()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Template CSV", Application.dataPath, "so_import_template", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("# ═══════════════════════════════════════════════════════════════");
            sb.AppendLine("# CSV ScriptableObject Import Template");
            sb.AppendLine("# ═══════════════════════════════════════════════════════════════");
            sb.AppendLine("#");
            sb.AppendLine("# Sections start with:  #Type,TypeName");
            sb.AppendLine("# Next line:            column headers (must match C# field names)");
            sb.AppendLine("# Following lines:      data rows");
            sb.AppendLine("#");
            sb.AppendLine("# _assetName (required)  = .asset file name");
            sb.AppendLine("# _assetPath (optional)  = subfolder inside the output folder");
            sb.AppendLine("#");
            sb.AppendLine("# Value formats:");
            sb.AppendLine("#   Color        →  #RRGGBB or #RRGGBBAA");
            sb.AppendLine("#   Vector2Int   →  x;y");
            sb.AppendLine("#   Ingredients  →  ItemName:Amount|ItemName:Amount");
            sb.AppendLine("#   LootDrops    →  ItemName:min:max:chance|...");
            sb.AppendLine("#   SO refs      →  asset name (must exist or be defined earlier in the CSV)");
            sb.AppendLine("#   Quoted fields → use double quotes for values containing commas");
            sb.AppendLine("#");
            sb.AppendLine("# Tip: Define items before recipes so references resolve correctly.");
            sb.AppendLine();

            Type[] types =
            {
                typeof(RawMaterialData),
                typeof(EssenceData),
                typeof(PillData),
                typeof(RecipeData),
                typeof(MachineData),
                typeof(RealmDefinition),
                typeof(EnemyData),
                typeof(OreVeinData),
                typeof(RecipeDatabase),
            };

            foreach (Type type in types)
            {
                sb.AppendLine($"#Type,{type.Name}");

                List<FieldInfo> fields = GetSerializedFields(type);
                var headers = new List<string> { "_assetName" };
                headers.AddRange(fields.Select(f => f.Name));
                sb.AppendLine(string.Join(",", headers));

                // Example row with placeholder values
                var placeholders = new List<string> { $"New{type.Name}" };
                placeholders.AddRange(fields.Select(GetPlaceholder));
                sb.AppendLine(string.Join(",", placeholders));

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
            Log($"Template exported to: {path}");
        }

        /// <summary>
        /// Returns all serialized fields of a type, walking up the hierarchy
        /// to <see cref="UnityEngine.Object"/>.
        /// </summary>
        private static List<FieldInfo> GetSerializedFields(Type type)
        {
            var fields = new List<FieldInfo>();
            Type current = type;
            while (current != null && current != typeof(UnityEngine.Object))
            {
                FieldInfo[] declared = current.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in declared)
                {
                    bool serialized = field.IsPublic
                        || Attribute.IsDefined(field, typeof(SerializeField));
                    if (!serialized) continue;
                    if (Attribute.IsDefined(field, typeof(HideInInspector))) continue;
                    fields.Add(field);
                }
                current = current.BaseType;
            }
            return fields;
        }

        private static string GetPlaceholder(FieldInfo field)
        {
            Type t = field.FieldType;
            if (t == typeof(string))  return "";
            if (t == typeof(int))     return "0";
            if (t == typeof(float))   return "0";
            if (t == typeof(double))  return "0";
            if (t == typeof(bool))    return "false";
            if (t.IsEnum)             return Enum.GetNames(t).FirstOrDefault() ?? "";
            if (t == typeof(Color))   return "#FFFFFF";
            if (t == typeof(Vector2Int)) return "0;0";
            if (t == typeof(Vector2)) return "0;0";
            if (t == typeof(Vector3)) return "0;0;0";
            if (t == typeof(List<RecipeIngredient>) || t == typeof(RecipeIngredient[])) return "ItemName:1";
            if (t == typeof(LootDrop[])) return "ItemName:1:3:0.5";
            if (typeof(ScriptableObject).IsAssignableFrom(t)) return "";
            if (t == typeof(Sprite) || t == typeof(GameObject)) return "";
            return "";
        }

        // ─── Utility ──────────────────────────────────────────────────────

        /// <summary>Creates all intermediate folders along the given asset path.</summary>
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>Removes characters that are invalid in file names.</summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (Array.IndexOf(invalid, c) < 0)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private void Log(string msg)
        {
            _log.Add(msg);
            Debug.Log($"[CSV Importer] {msg}");
        }
    }
}
