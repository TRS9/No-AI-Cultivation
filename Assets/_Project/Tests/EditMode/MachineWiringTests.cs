using System;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;
using NUnit.Framework;
using UnityEngine;

namespace CultivationGame.Tests
{
    public class MachineWiringTests
    {
        [TestCase(MachineType.Furnace, typeof(BaseMachine))]
        [TestCase(MachineType.Crusher, typeof(BaseMachine))]
        [TestCase(MachineType.Mixer, typeof(BaseMachine))]
        [TestCase(MachineType.Distiller, typeof(BaseMachine))]
        [TestCase(MachineType.Condenser, typeof(BaseMachine))]
        [TestCase(MachineType.PillPress, typeof(BaseMachine))]
        [TestCase(MachineType.Storage, typeof(StorageContainer))]
        [TestCase(MachineType.SpiritPipe, typeof(SpiritPipe))]
        [TestCase(MachineType.ResourceExtractor, typeof(ResourceExtractor))]
        [TestCase(MachineType.Splitter, typeof(Splitter))]
        [TestCase(MachineType.Merger, typeof(Merger))]
        [TestCase(MachineType.QiConduit, typeof(QiConduit))]
        public void VisualOnlyPrefabReceivesBehaviorAndDataExactlyOnce(MachineType type, Type componentType)
        {
            var go = new GameObject("Visual-only machine");
            var data = ScriptableObject.CreateInstance<MachineData>();
            data.machineType = type;
            try
            {
                Assert.That(MachineWiring.Wire(go, data), Is.True);
                Assert.That(MachineWiring.Wire(go, data), Is.True);
                Assert.That(go.GetComponents(componentType).Length, Is.EqualTo(1));
                var component = go.GetComponent(componentType);
                Assert.That(componentType.GetProperty("MachineData").GetValue(component), Is.SameAs(data));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }
    }
}
