using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Tests
{
    [TestFixture]
    public class RecipeDataValidationTests
    {
        private TestItemData _ironOre;
        private TestItemData _ironIngot;

        [SetUp]
        public void SetUp()
        {
            _ironOre = RecipeTestHelper.CreateItem("IronOre");
            _ironIngot = RecipeTestHelper.CreateItem("IronIngot");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ironOre);
            Object.DestroyImmediate(_ironIngot);
        }

        [Test]
        public void IsValid_WithEmptyInputs_ReturnsFalse()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Empty Inputs",
                inputs: new RecipeIngredient[0],
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } });

            Assert.IsFalse(recipe.IsValid);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void IsValid_WithNullInputs_ReturnsFalse()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Null Inputs",
                inputs: null,
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } });

            Assert.IsFalse(recipe.IsValid);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void IsValid_WithNoOutputs_ReturnsFalse()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "No Output",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 2 } },
                outputs: new RecipeIngredient[0]);

            Assert.IsFalse(recipe.IsValid);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void IsValid_WithNullOutputs_ReturnsFalse()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Null Output",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 2 } },
                outputs: null);

            Assert.IsFalse(recipe.IsValid);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Validate_CraftingDurationZero_ReturnsWarning()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Zero Duration",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 1 } },
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } },
                duration: 0f);

            var messages = recipe.Validate();

            Assert.IsTrue(messages.Any(m => m.Contains("craftingDuration")));

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Validate_CraftingDurationNegative_ReturnsWarning()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Negative Duration",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 1 } },
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } },
                duration: -1f);

            var messages = recipe.Validate();

            Assert.IsTrue(messages.Any(m => m.Contains("craftingDuration")));

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void RequiredMachine_IsCorrectlySet()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Furnace Recipe",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 1 } },
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } },
                machine: MachineType.Furnace);

            Assert.AreEqual(MachineType.Furnace, recipe.requiredMachine);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void IsValid_WithValidRecipe_ReturnsTrue()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Valid Recipe",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 2 } },
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } });

            Assert.IsTrue(recipe.IsValid);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Validate_ValidRecipe_ReturnsNoMessages()
        {
            var recipe = RecipeTestHelper.CreateRecipe(
                "Valid Recipe",
                inputs: new[] { new RecipeIngredient { item = _ironOre, amount = 2 } },
                outputs: new[] { new RecipeIngredient { item = _ironIngot, amount = 1 } },
                duration: 5f);

            var messages = recipe.Validate();

            Assert.IsEmpty(messages);

            Object.DestroyImmediate(recipe);
        }
    }
}
