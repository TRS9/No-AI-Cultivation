using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CultivationGame.Data;

namespace CultivationGame.Editor
{
    public class RecipeDatabaseWindow : EditorWindow
    {
        // ── Tab state ──────────────────────────────────────────────────────
        private enum Tab { Overview, Validation, Balance }
        private Tab _currentTab;
        private static readonly string[] TabLabels = { "Overview", "Validation", "Balance Analysis" };

        // ── Data ───────────────────────────────────────────────────────────
        private RecipeDatabase[] _databases;
        private RecipeData[] _allRecipes;

        // ── Validation cache ───────────────────────────────────────────────
        private List<ValidationEntry> _validationResults;

        // ── Balance cache ──────────────────────────────────────────────────
        private List<BalanceEntry> _balanceResults;
        private List<string> _bottleneckItems;

        // ── Scroll positions ───────────────────────────────────────────────
        private Vector2 _overviewScroll;
        private Vector2 _validationScroll;
        private Vector2 _balanceScroll;

        // ── Styles ─────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        private bool _stylesReady;

        // ── Constants ──────────────────────────────────────────────────────
        private static readonly Color HeaderColor = new Color(0.22f, 0.38f, 0.54f);

        // ════════════════════════════════════════════════════════════════════
        // Menu & Lifecycle
        // ════════════════════════════════════════════════════════════════════

        [MenuItem("Tools/Recipe Database")]
        public static void Open()
        {
            var window = GetWindow<RecipeDatabaseWindow>("Recipe Database");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable() => RefreshData();

        private void OnFocus() => RefreshData();

        // ════════════════════════════════════════════════════════════════════
        // Data Loading
        // ════════════════════════════════════════════════════════════════════

        private void RefreshData()
        {
            // Find all RecipeDatabase assets
            var dbGuids = AssetDatabase.FindAssets("t:RecipeDatabase");
            _databases = dbGuids
                .Select(g => AssetDatabase.LoadAssetAtPath<RecipeDatabase>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(db => db != null)
                .ToArray();

            // Collect all unique recipes across databases
            var recipeSet = new HashSet<RecipeData>();
            foreach (var db in _databases)
            {
                if (db.allRecipes == null) continue;
                foreach (var r in db.allRecipes)
                {
                    if (r != null) recipeSet.Add(r);
                }
            }

            // Also find standalone RecipeData assets not referenced in any database
            var recipeGuids = AssetDatabase.FindAssets("t:RecipeData");
            foreach (var g in recipeGuids)
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(g));
                if (recipe != null) recipeSet.Add(recipe);
            }

            _allRecipes = recipeSet.OrderBy(r => r.name).ToArray();

            RunValidation();
            RunBalanceAnalysis();
        }

        // ════════════════════════════════════════════════════════════════════
        // GUI
        // ════════════════════════════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                padding = new RectOffset(6, 0, 0, 0),
            };

            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) },
                wordWrap = true,
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.75f, 0.2f) },
                wordWrap = true,
            };

            _stylesReady = true;
        }

        private void OnGUI()
        {
            EnsureStyles();

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, TabLabels, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshData();
            EditorGUILayout.EndHorizontal();

            if (_allRecipes == null || _allRecipes.Length == 0)
            {
                EditorGUILayout.HelpBox("No RecipeData assets found in the project.", MessageType.Info);
                return;
            }

            switch (_currentTab)
            {
                case Tab.Overview:
                    DrawOverviewTab();
                    break;
                case Tab.Validation:
                    DrawValidationTab();
                    break;
                case Tab.Balance:
                    DrawBalanceTab();
                    break;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Overview Tab
        // ════════════════════════════════════════════════════════════════════

        private void DrawOverviewTab()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Total Recipes: {_allRecipes.Length}", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Column headers
            DrawSectionHeader("Recipe | Machine | Inputs | Outputs | Time");

            _overviewScroll = EditorGUILayout.BeginScrollView(_overviewScroll);

            for (int i = 0; i < _allRecipes.Length; i++)
            {
                var recipe = _allRecipes[i];
                if (recipe == null) continue;

                bool hasError = HasValidationError(recipe);
                DrawRecipeRow(recipe, i, hasError);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRecipeRow(RecipeData recipe, int index, bool hasError)
        {
            var bgColor = GUI.backgroundColor;
            if (hasError)
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f, 0.15f);

            EditorGUILayout.BeginHorizontal(index % 2 == 0 ? "box" : "box");
            GUI.backgroundColor = bgColor;

            // Recipe Name (clickable)
            string displayName = !string.IsNullOrEmpty(recipe.recipeName) ? recipe.recipeName : recipe.name;
            if (GUILayout.Button(displayName, EditorStyles.linkLabel, GUILayout.Width(140)))
                SelectRecipe(recipe);

            // Machine
            EditorGUILayout.LabelField(recipe.requiredMachine.ToString(), GUILayout.Width(100));

            // Inputs
            EditorGUILayout.LabelField(FormatIngredients(recipe.inputs), GUILayout.Width(180));

            // Outputs
            EditorGUILayout.LabelField(FormatIngredients(recipe.outputs), GUILayout.Width(180));

            // Duration
            EditorGUILayout.LabelField(FormatDuration(recipe.craftingDuration), GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();

            // Handle double-click
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
            {
                var lastRect = GUILayoutUtility.GetLastRect();
                if (lastRect.Contains(Event.current.mousePosition))
                {
                    SelectRecipe(recipe);
                    Event.current.Use();
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Validation Tab
        // ════════════════════════════════════════════════════════════════════

        private struct ValidationEntry
        {
            public RecipeData Recipe;
            public string Message;
            public ValidationSeverity Severity;
        }

        private enum ValidationSeverity { Error, Warning }

        private void DrawValidationTab()
        {
            EditorGUILayout.Space(4);

            int errorCount = _validationResults.Count(v => v.Severity == ValidationSeverity.Error);
            int warningCount = _validationResults.Count(v => v.Severity == ValidationSeverity.Warning);

            if (_validationResults.Count == 0)
            {
                EditorGUILayout.HelpBox("All recipes passed validation.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(
                $"Found {errorCount} error(s) and {warningCount} warning(s)",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Errors first
            if (errorCount > 0)
            {
                DrawSectionHeader("Errors");
                _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll);

                foreach (var entry in _validationResults.Where(v => v.Severity == ValidationSeverity.Error))
                    DrawValidationEntry(entry);

                if (warningCount > 0)
                {
                    EditorGUILayout.Space(8);
                    DrawSectionHeader("Warnings");
                    foreach (var entry in _validationResults.Where(v => v.Severity == ValidationSeverity.Warning))
                        DrawValidationEntry(entry);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                DrawSectionHeader("Warnings");
                _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll);
                foreach (var entry in _validationResults)
                    DrawValidationEntry(entry);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawValidationEntry(ValidationEntry entry)
        {
            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = entry.Severity == ValidationSeverity.Error
                ? new Color(1f, 0.3f, 0.3f, 0.2f)
                : new Color(1f, 0.75f, 0.2f, 0.2f);

            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = bgColor;

            string icon = entry.Severity == ValidationSeverity.Error ? "✖" : "⚠";
            var style = entry.Severity == ValidationSeverity.Error ? _errorStyle : _warningStyle;

            string recipeName = entry.Recipe != null
                ? (!string.IsNullOrEmpty(entry.Recipe.recipeName) ? entry.Recipe.recipeName : entry.Recipe.name)
                : "(null)";

            if (entry.Recipe != null && GUILayout.Button(recipeName, EditorStyles.linkLabel, GUILayout.Width(140)))
                SelectRecipe(entry.Recipe);
            else if (entry.Recipe == null)
                EditorGUILayout.LabelField("(null)", GUILayout.Width(140));

            EditorGUILayout.LabelField($"{icon} {entry.Message}", style);

            EditorGUILayout.EndHorizontal();
        }

        private void RunValidation()
        {
            _validationResults = new List<ValidationEntry>();

            foreach (var recipe in _allRecipes)
            {
                if (recipe == null)
                {
                    _validationResults.Add(new ValidationEntry
                    {
                        Recipe = null,
                        Message = "Null recipe reference in database",
                        Severity = ValidationSeverity.Error
                    });
                    continue;
                }

                // Check: Recipe without outputs
                if (recipe.outputs == null || recipe.outputs.Count == 0)
                {
                    _validationResults.Add(new ValidationEntry
                    {
                        Recipe = recipe,
                        Message = "Recipe has no outputs",
                        Severity = ValidationSeverity.Error
                    });
                }

                // Check: Recipe without inputs
                if (recipe.inputs == null || recipe.inputs.Count == 0)
                {
                    _validationResults.Add(new ValidationEntry
                    {
                        Recipe = recipe,
                        Message = "Recipe has no inputs",
                        Severity = ValidationSeverity.Warning
                    });
                }

                // Check: Null item references in inputs
                if (recipe.inputs != null)
                {
                    for (int i = 0; i < recipe.inputs.Count; i++)
                    {
                        if (recipe.inputs[i].item == null)
                        {
                            _validationResults.Add(new ValidationEntry
                            {
                                Recipe = recipe,
                                Message = $"Input [{i}] references a missing/null item",
                                Severity = ValidationSeverity.Error
                            });
                        }
                        else if (recipe.inputs[i].amount <= 0)
                        {
                            _validationResults.Add(new ValidationEntry
                            {
                                Recipe = recipe,
                                Message = $"Input [{i}] ({recipe.inputs[i].item.name}) has invalid amount: {recipe.inputs[i].amount}",
                                Severity = ValidationSeverity.Error
                            });
                        }
                    }
                }

                // Check: Null item references in outputs
                if (recipe.outputs != null)
                {
                    for (int i = 0; i < recipe.outputs.Count; i++)
                    {
                        if (recipe.outputs[i].item == null)
                        {
                            _validationResults.Add(new ValidationEntry
                            {
                                Recipe = recipe,
                                Message = $"Output [{i}] references a missing/null item",
                                Severity = ValidationSeverity.Error
                            });
                        }
                        else if (recipe.outputs[i].amount <= 0)
                        {
                            _validationResults.Add(new ValidationEntry
                            {
                                Recipe = recipe,
                                Message = $"Output [{i}] ({recipe.outputs[i].item.name}) has invalid amount: {recipe.outputs[i].amount}",
                                Severity = ValidationSeverity.Error
                            });
                        }
                    }
                }

                // Check: Crafting duration
                if (recipe.craftingDuration <= 0)
                {
                    _validationResults.Add(new ValidationEntry
                    {
                        Recipe = recipe,
                        Message = "Crafting duration is zero or negative",
                        Severity = ValidationSeverity.Warning
                    });
                }
            }

            // Check: Duplicate recipes (same inputs + machine)
            CheckDuplicateRecipes();

            // Check: Circular dependencies
            CheckCircularDependencies();
        }

        private void CheckDuplicateRecipes()
        {
            for (int i = 0; i < _allRecipes.Length; i++)
            {
                var a = _allRecipes[i];
                if (a == null || a.inputs == null) continue;

                for (int j = i + 1; j < _allRecipes.Length; j++)
                {
                    var b = _allRecipes[j];
                    if (b == null || b.inputs == null) continue;

                    if (a.requiredMachine != b.requiredMachine) continue;
                    if (a.inputs.Count != b.inputs.Count) continue;

                    if (IngredientsMatch(a.inputs, b.inputs))
                    {
                        string nameA = !string.IsNullOrEmpty(a.recipeName) ? a.recipeName : a.name;
                        string nameB = !string.IsNullOrEmpty(b.recipeName) ? b.recipeName : b.name;
                        _validationResults.Add(new ValidationEntry
                        {
                            Recipe = a,
                            Message = $"Duplicate: same inputs and machine as '{nameB}'",
                            Severity = ValidationSeverity.Warning
                        });
                    }
                }
            }
        }

        private static bool IngredientsMatch(List<RecipeIngredient> a, List<RecipeIngredient> b)
        {
            if (a.Count != b.Count) return false;

            var sortedA = a.OrderBy(x => x.item != null ? x.item.GetInstanceID() : 0).ToList();
            var sortedB = b.OrderBy(x => x.item != null ? x.item.GetInstanceID() : 0).ToList();

            for (int i = 0; i < sortedA.Count; i++)
            {
                if (sortedA[i].item != sortedB[i].item) return false;
                if (sortedA[i].amount != sortedB[i].amount) return false;
            }
            return true;
        }

        private void CheckCircularDependencies()
        {
            // Build a map: item → recipes that produce it, item → recipes that consume it
            var producedBy = new Dictionary<ItemData, List<RecipeData>>();
            var consumedBy = new Dictionary<ItemData, List<RecipeData>>();

            foreach (var recipe in _allRecipes)
            {
                if (recipe == null) continue;

                if (recipe.outputs != null)
                {
                    foreach (var output in recipe.outputs)
                    {
                        if (output.item == null) continue;
                        if (!producedBy.ContainsKey(output.item))
                            producedBy[output.item] = new List<RecipeData>();
                        producedBy[output.item].Add(recipe);
                    }
                }

                if (recipe.inputs != null)
                {
                    foreach (var input in recipe.inputs)
                    {
                        if (input.item == null) continue;
                        if (!consumedBy.ContainsKey(input.item))
                            consumedBy[input.item] = new List<RecipeData>();
                        consumedBy[input.item].Add(recipe);
                    }
                }
            }

            // For each recipe, check if any output item is also an input item of a recipe
            // that produces one of this recipe's inputs (A→B, B→A cycle)
            var reportedCycles = new HashSet<string>();
            foreach (var recipe in _allRecipes)
            {
                if (recipe == null || recipe.outputs == null || recipe.inputs == null) continue;

                foreach (var output in recipe.outputs)
                {
                    if (output.item == null) continue;
                    if (!consumedBy.ContainsKey(output.item)) continue;

                    foreach (var consumer in consumedBy[output.item])
                    {
                        if (consumer == null || consumer.outputs == null) continue;

                        foreach (var consumerOutput in consumer.outputs)
                        {
                            if (consumerOutput.item == null) continue;

                            foreach (var input in recipe.inputs)
                            {
                                if (input.item == consumerOutput.item)
                                {
                                    string nameA = !string.IsNullOrEmpty(recipe.recipeName)
                                        ? recipe.recipeName : recipe.name;
                                    string nameB = !string.IsNullOrEmpty(consumer.recipeName)
                                        ? consumer.recipeName : consumer.name;

                                    // Avoid reporting both directions
                                    string cycleKey = string.Compare(nameA, nameB, StringComparison.Ordinal) < 0
                                        ? $"{nameA}↔{nameB}" : $"{nameB}↔{nameA}";

                                    if (reportedCycles.Add(cycleKey))
                                    {
                                        _validationResults.Add(new ValidationEntry
                                        {
                                            Recipe = recipe,
                                            Message = $"Circular dependency: '{nameA}' ↔ '{nameB}' " +
                                                      $"(via {output.item.name} / {consumerOutput.item.name})",
                                            Severity = ValidationSeverity.Warning
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool HasValidationError(RecipeData recipe)
        {
            return _validationResults != null &&
                   _validationResults.Any(v => v.Recipe == recipe && v.Severity == ValidationSeverity.Error);
        }

        // ════════════════════════════════════════════════════════════════════
        // Balance Analysis Tab
        // ════════════════════════════════════════════════════════════════════

        private struct BalanceEntry
        {
            public RecipeData Recipe;
            public int TotalInputQi;
            public int TotalOutputQi;
            public float Ratio; // output / input
            public double QiCost;
        }

        private void RunBalanceAnalysis()
        {
            _balanceResults = new List<BalanceEntry>();
            var producerCount = new Dictionary<ItemData, int>();

            foreach (var recipe in _allRecipes)
            {
                if (recipe == null) continue;

                int inputQi = 0;
                int outputQi = 0;

                if (recipe.inputs != null)
                {
                    foreach (var inp in recipe.inputs)
                    {
                        if (inp.item != null)
                            inputQi += inp.item.qiValue * inp.amount;
                    }
                }

                if (recipe.outputs != null)
                {
                    foreach (var outp in recipe.outputs)
                    {
                        if (outp.item != null)
                        {
                            outputQi += outp.item.qiValue * outp.amount;

                            if (!producerCount.ContainsKey(outp.item))
                                producerCount[outp.item] = 0;
                            producerCount[outp.item]++;
                        }
                    }
                }

                _balanceResults.Add(new BalanceEntry
                {
                    Recipe = recipe,
                    TotalInputQi = inputQi,
                    TotalOutputQi = outputQi,
                    Ratio = inputQi > 0 ? (float)outputQi / inputQi : 0f,
                    QiCost = recipe.qiCost,
                });
            }

            // Bottleneck: items produced by only one recipe
            _bottleneckItems = new List<string>();
            foreach (var kvp in producerCount)
            {
                if (kvp.Value == 1 && kvp.Key != null)
                    _bottleneckItems.Add(kvp.Key.name);
            }
            _bottleneckItems.Sort();
        }

        private void DrawBalanceTab()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Balance Analysis", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            _balanceScroll = EditorGUILayout.BeginScrollView(_balanceScroll);

            // Input/Output Ratio Table
            DrawSectionHeader("Input/Output Ratio & Qi Costs");
            EditorGUILayout.Space(2);

            // Header row
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Recipe", EditorStyles.boldLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("Input Qi", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Output Qi", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Ratio", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Qi Cost", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Net Value", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _balanceResults.Count; i++)
            {
                var entry = _balanceResults[i];
                if (entry.Recipe == null) continue;

                EditorGUILayout.BeginHorizontal("box");

                string displayName = !string.IsNullOrEmpty(entry.Recipe.recipeName)
                    ? entry.Recipe.recipeName : entry.Recipe.name;
                if (GUILayout.Button(displayName, EditorStyles.linkLabel, GUILayout.Width(140)))
                    SelectRecipe(entry.Recipe);

                EditorGUILayout.LabelField(entry.TotalInputQi.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(entry.TotalOutputQi.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(entry.Ratio.ToString("F2"), GUILayout.Width(60));
                EditorGUILayout.LabelField(entry.QiCost.ToString("F1"), GUILayout.Width(70));

                double netValue = entry.TotalOutputQi - entry.TotalInputQi - entry.QiCost;
                var color = GUI.contentColor;
                GUI.contentColor = netValue >= 0 ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
                EditorGUILayout.LabelField(netValue.ToString("F1"), GUILayout.Width(70));
                GUI.contentColor = color;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);

            // Bottleneck Analysis
            DrawSectionHeader("Bottleneck Items (produced by only one recipe)");
            EditorGUILayout.Space(2);

            if (_bottleneckItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No bottleneck items found — all produced items have multiple sources.",
                    MessageType.Info);
            }
            else
            {
                foreach (var itemName in _bottleneckItems)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
                    EditorGUILayout.LabelField(itemName);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        private static string FormatIngredients(List<RecipeIngredient> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return "—";

            return string.Join(", ", ingredients.Select(ing =>
            {
                string itemName = ing.item != null ? ing.item.name : "<missing>";
                return $"{ing.amount}x {itemName}";
            }));
        }

        private static string FormatDuration(float seconds)
        {
            if (seconds <= 0) return "—";
            return seconds < 60 ? $"{seconds:F1}s" : $"{seconds / 60:F1}m";
        }

        private static void SelectRecipe(RecipeData recipe)
        {
            Selection.activeObject = recipe;
            EditorGUIUtility.PingObject(recipe);
        }

        private void DrawSectionHeader(string label)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, HeaderColor);
            rect.xMin += 4;
            GUI.Label(rect, label, _headerStyle);
            GUILayout.Space(2);
        }
    }
}
