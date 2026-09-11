using System.Collections.Generic;
using System.Reflection;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;
using CultivationGame.UI;
using NUnit.Framework;
using UnityEngine;

namespace CultivationGame.Tests
{
    public class BaseMachineTests
    {
        private readonly List<Object> _objects = new();
        private TestItemData _ore, _ingot, _pill;
        private MachineData _data;
        private BaseMachine _machine;
        private RecipeData _recipe;

        private T Keep<T>(T obj) where T : Object { _objects.Add(obj); return obj; }

        [SetUp]
        public void SetUp()
        {
            _ore = Keep(RecipeTestHelper.CreateItem("Ore"));
            _ingot = Keep(RecipeTestHelper.CreateItem("Ingot"));
            _pill = Keep(RecipeTestHelper.CreateItem("Pill"));
            _data = Keep(ScriptableObject.CreateInstance<MachineData>());
            _data.machineType = MachineType.Furnace;
            _data.processingSpeed = 1f;
            _recipe = MakeRecipe(_ingot);
            _recipe.name = "Smelt";
            _machine = MakeMachine();
            _machine.SetRecipe(_recipe);
        }

        private RecipeData MakeRecipe(ItemData output) => Keep(RecipeTestHelper.CreateRecipe("Recipe",
            new[] { new RecipeIngredient { item = _ore, amount = 2 } },
            new[] { new RecipeIngredient { item = output, amount = 1 } }));

        private BaseMachine MakeMachine()
        {
            var go = Keep(new GameObject("Test machine"));
            var machine = go.AddComponent<BaseMachine>();
            // EditMode does not run the normal scene lifecycle.
            Invoke(machine, "Awake");
            machine.SetMachineData(_data);
            machine.IsPowered = true;
            return machine;
        }

        private static void Invoke(BaseMachine machine, string method) =>
            typeof(BaseMachine).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(machine, null);

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void RecipeChangeDoesNotChangePaidBatchOutput()
        {
            _machine.AddInput(_ore, 2);
            Invoke(_machine, "TryStartProcessing");
            var nextRecipe = MakeRecipe(_pill);
            _machine.SetRecipe(nextRecipe);
            Invoke(_machine, "CompleteProcessing");
            Assert.That(_machine.OutputInventory.HasItem(_ingot), Is.True);
            Assert.That(_machine.OutputInventory.HasItem(_pill), Is.False);
            Assert.That(_machine.CurrentRecipe, Is.SameAs(nextRecipe));
            Assert.That(_machine.InputInventory.TotalCount(), Is.Zero);
        }

        [Test]
        public void FullOutputRetainsBatchUntilSpaceIsAvailable()
        {
            _machine.AddInput(_ore, 2);
            Invoke(_machine, "TryStartProcessing");
            _machine.OutputInventory.TryAdd(_pill, _machine.OutputInventory.MaxCapacity);
            Invoke(_machine, "CompleteProcessing");
            Assert.That(_machine.IsProcessing, Is.True);
            _machine.OutputInventory.Clear();
            Invoke(_machine, "CompleteProcessing");
            Assert.That(_machine.OutputInventory.HasItem(_ingot), Is.True);
            Assert.That(_machine.IsProcessing, Is.False);
            Invoke(_machine, "CompleteProcessing");
            Assert.That(_machine.OutputInventory.TotalCount(), Is.EqualTo(1));
        }

        [Test]
        public void DuplicateInputsRequireCombinedQuantity()
        {
            _recipe.inputs.Add(new RecipeIngredient { item = _ore, amount = 2 });
            _machine.AddInput(_ore, 2);
            Invoke(_machine, "TryStartProcessing");
            Assert.That(_machine.IsProcessing, Is.False);
            Assert.That(_machine.InputInventory.TotalCount(), Is.EqualTo(2));
            _machine.AddInput(_ore, 2);
            Invoke(_machine, "TryStartProcessing");
            Assert.That(_machine.IsProcessing, Is.True);
            Assert.That(_machine.InputInventory.TotalCount(), Is.Zero);
        }

        [Test]
        public void WrongMachineOrMissingPowerDoesNotConsumeInputs()
        {
            _machine.AddInput(_ore, 2);
            _recipe.requiredMachine = MachineType.Mixer;
            Invoke(_machine, "TryStartProcessing");
            Assert.That(_machine.IsProcessing, Is.False);
            _recipe.requiredMachine = MachineType.Furnace;
            _machine.IsPowered = false;
            Invoke(_machine, "TryStartProcessing");
            Assert.That(_machine.IsProcessing, Is.False);
            Assert.That(_machine.InputInventory.TotalCount(), Is.EqualTo(2));
        }

        [Test]
        public void SerializedBatchResumesWithoutPayingTwice()
        {
            _machine.AddInput(_ore, 2);
            Invoke(_machine, "TryStartProcessing");
            var saved = new MachineInventorySaveEntry {
                recipeId = _recipe.name, processingRecipeId = _machine.ProcessingRecipe.name,
                processingTimer = 2f, processingDuration = _machine.ProcessingDuration
            };
            var loaded = JsonUtility.FromJson<MachineInventorySaveEntry>(JsonUtility.ToJson(saved));
            var restored = MakeMachine();
            restored.SetRecipe(_recipe);
            Assert.That(loaded.processingRecipeId, Is.EqualTo(_recipe.name));
            restored.RestoreProcessing(_recipe, loaded.processingTimer, loaded.processingDuration);
            Assert.That(restored.ProcessingTimer, Is.EqualTo(2f));
            Assert.That(restored.ProcessingProgress, Is.EqualTo(0.4f).Within(0.001f));
            Invoke(restored, "CompleteProcessing");
            Assert.That(restored.OutputInventory.HasItem(_ingot), Is.True);
            Assert.That(restored.InputInventory.TotalCount(), Is.Zero);
        }

        [Test]
        public void SaveManagerRoundTripKeepsActiveAndSelectedRecipesSeparate()
        {
            _machine.AddInput(_ore, 2);
            Invoke(_machine, "TryStartProcessing");
            _machine.RestoreProcessing(_recipe, 2f, 5f);
            var next = MakeRecipe(_pill);
            next.name = "NextRecipe";
            _machine.SetRecipe(next);
            var manager = Keep(new GameObject("Save coordinator test")).AddComponent<SaveManager>();
            manager.allItems = new List<ItemData> { _ore, _ingot, _pill };
            manager.recipeDatabase = Keep(RecipeTestHelper.CreateDatabase(_recipe, next));
            var data = new SaveData();
            typeof(SaveManager).GetMethod("SaveMachineInventory", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { data, _machine, "test-guid" });
            var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            var restored = MakeMachine();
            typeof(SaveManager).GetMethod("RestoreMachineInventories", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { loaded, new Dictionary<string, IMachineConnectable> { { "test-guid", restored } } });
            Assert.That(restored.CurrentRecipe, Is.SameAs(next));
            Assert.That(restored.ProcessingRecipe, Is.SameAs(_recipe));
            Assert.That(restored.ProcessingTimer, Is.EqualTo(2f));
            Invoke(restored, "CompleteProcessing");
            Assert.That(restored.OutputInventory.HasItem(_ingot), Is.True);
            Assert.That(restored.OutputInventory.HasItem(_pill), Is.False);
        }
    }
}
