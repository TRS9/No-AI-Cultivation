using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Tests
{
    [TestFixture]
    public class RecipeDatabaseLookupTests
    {
        private TestItemData _ironOre;
        private TestItemData _ironIngot;
        private TestItemData _copper;
        private TestItemData _copperIngot;

        private RecipeData _furnaceRecipe;
        private RecipeData _crusherRecipe;
        private RecipeData _furnaceRecipe2;
        private RecipeDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _ironOre = RecipeTestHelper.CreateItem("IronOre");
            _ironIngot = RecipeTestHelper.CreateItem("IronIngot");
            _copper = RecipeTestHelper.CreateItem("Copper");
            _copperIngot = RecipeTestHelper.CreateItem("CopperIngot");

            _furnaceRecipe = RecipeTestHelper.CreateRecipe(
                "Smelt Iron",
                new[] { new RecipeIngredient { item = _ironOre, amount = 2 } },
                new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } },
                MachineType.Furnace);

            _crusherRecipe = RecipeTestHelper.CreateRecipe(
                "Crush Copper",
                new[] { new RecipeIngredient { item = _copper, amount = 3 } },
                new[] { new RecipeIngredient { item = _copperIngot, amount = 1 } },
                MachineType.Crusher);

            _furnaceRecipe2 = RecipeTestHelper.CreateRecipe(
                "Smelt Copper",
                new[] { new RecipeIngredient { item = _copper, amount = 2 } },
                new[] { new RecipeIngredient { item = _copperIngot, amount = 1 } },
                MachineType.Furnace);

            _database = RecipeTestHelper.CreateDatabase(_furnaceRecipe, _crusherRecipe, _furnaceRecipe2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_furnaceRecipe);
            Object.DestroyImmediate(_crusherRecipe);
            Object.DestroyImmediate(_furnaceRecipe2);
            Object.DestroyImmediate(_ironOre);
            Object.DestroyImmediate(_ironIngot);
            Object.DestroyImmediate(_copper);
            Object.DestroyImmediate(_copperIngot);
        }

        // --- GetRecipesForMachine ---

        [Test]
        public void GetRecipesForMachine_ReturnsOnlyMatchingRecipes()
        {
            var furnaceRecipes = _database.GetRecipesForMachine(MachineType.Furnace);

            Assert.AreEqual(2, furnaceRecipes.Count);
            Assert.IsTrue(furnaceRecipes.Contains(_furnaceRecipe));
            Assert.IsTrue(furnaceRecipes.Contains(_furnaceRecipe2));
            Assert.IsFalse(furnaceRecipes.Contains(_crusherRecipe));
        }

        [Test]
        public void GetRecipesForMachine_NoMatch_ReturnsEmptyList()
        {
            var mixerRecipes = _database.GetRecipesForMachine(MachineType.Mixer);

            Assert.IsNotNull(mixerRecipes);
            Assert.AreEqual(0, mixerRecipes.Count);
        }

        // --- GetAllRecipes ---

        [Test]
        public void GetAllRecipes_ReturnsCompleteList()
        {
            var all = _database.GetAllRecipes();

            Assert.AreEqual(3, all.Count);
            Assert.IsTrue(all.Contains(_furnaceRecipe));
            Assert.IsTrue(all.Contains(_crusherRecipe));
            Assert.IsTrue(all.Contains(_furnaceRecipe2));
        }

        [Test]
        public void GetAllRecipes_EmptyDatabase_ReturnsEmptyList()
        {
            var emptyDb = RecipeTestHelper.CreateDatabase();

            var all = emptyDb.GetAllRecipes();

            Assert.IsNotNull(all);
            Assert.AreEqual(0, all.Count);

            Object.DestroyImmediate(emptyDb);
        }

        [Test]
        public void GetAllRecipes_NullList_ReturnsEmptyList()
        {
            var db = ScriptableObject.CreateInstance<RecipeDatabase>();
            db.allRecipes = null;

            var all = db.GetAllRecipes();

            Assert.IsNotNull(all);
            Assert.AreEqual(0, all.Count);

            Object.DestroyImmediate(db);
        }

        // --- FindRecipe ---

        [Test]
        public void FindRecipe_ExactMatch_ReturnsCorrectRecipe()
        {
            var inputs = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = _ironOre, amount = 2 }
            };

            var result = _database.FindRecipe(inputs, MachineType.Furnace);

            Assert.IsNotNull(result);
            Assert.AreEqual("Smelt Iron", result.recipeName);
        }

        [Test]
        public void FindRecipe_NoMatch_ReturnsNull()
        {
            var inputs = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = _ironOre, amount = 2 }
            };

            var result = _database.FindRecipe(inputs, MachineType.Mixer);

            Assert.IsNull(result);
        }

        [Test]
        public void FindRecipe_NullInputs_ReturnsNull()
        {
            var result = _database.FindRecipe(null, MachineType.Furnace);

            Assert.IsNull(result);
        }

        [Test]
        public void FindRecipe_SubsetInputs_ReturnsNull()
        {
            // Recipe needs [IronOre x2], but we provide [IronOre x2, Copper x1] — extra input
            var inputs = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = _ironOre, amount = 2 },
                new RecipeIngredient { item = _copper, amount = 1 }
            };

            var result = _database.FindRecipe(inputs, MachineType.Furnace);

            Assert.IsNull(result);
        }

        [Test]
        public void FindRecipe_WrongAmounts_ReturnsNull()
        {
            // Recipe needs [IronOre x2], but we provide [IronOre x5]
            var inputs = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = _ironOre, amount = 5 }
            };

            var result = _database.FindRecipe(inputs, MachineType.Furnace);

            Assert.IsNull(result);
        }

        [Test]
        public void FindRecipe_FewerInputsThanRequired_ReturnsNull()
        {
            // Recipe needs [Copper x3] for crusher, we give empty
            var inputs = new List<RecipeIngredient>();

            var result = _database.FindRecipe(inputs, MachineType.Crusher);

            Assert.IsNull(result);
        }

        // --- Duplicates ---

        [Test]
        public void FindRecipe_Duplicates_ReturnsFirstMatch()
        {
            // Add a duplicate of _furnaceRecipe
            var duplicate = RecipeTestHelper.CreateRecipe(
                "Smelt Iron Duplicate",
                new[] { new RecipeIngredient { item = _ironOre, amount = 2 } },
                new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } },
                MachineType.Furnace);

            var dbWithDup = RecipeTestHelper.CreateDatabase(_furnaceRecipe, duplicate);

            var inputs = new List<RecipeIngredient>
            {
                new RecipeIngredient { item = _ironOre, amount = 2 }
            };

            var result = dbWithDup.FindRecipe(inputs, MachineType.Furnace);

            Assert.IsNotNull(result);
            // FirstOrDefault should return the first one added
            Assert.AreEqual("Smelt Iron", result.recipeName);

            Object.DestroyImmediate(duplicate);
            Object.DestroyImmediate(dbWithDup);
        }

        // --- GetRecipesForMachine with null allRecipes ---

        [Test]
        public void GetRecipesForMachine_NullAllRecipes_ReturnsEmptyList()
        {
            var db = ScriptableObject.CreateInstance<RecipeDatabase>();
            db.allRecipes = null;

            var result = db.GetRecipesForMachine(MachineType.Furnace);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            Object.DestroyImmediate(db);
        }
    }
}
