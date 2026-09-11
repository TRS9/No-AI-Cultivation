using System;
using CultivationGame.Core;
using CultivationGame.Systems;
using NUnit.Framework;
using UnityEngine;

namespace CultivationGame.Tests
{
    public class PortalPersistenceTests
    {
        private sealed class StopBeforeUnload : Exception { }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void PortalsCaptureBeforeChangingSceneIdentity(bool realmPortal, bool exitPortal)
        {
            SceneTransitionData.ResetAll();
            SceneTransitionData.SetRealm(BiomeType.SpiritForest, 123);
            SceneTransitionData.SetReturn("Universe", Vector3.one, 45f);
            var go = new GameObject("Portal persistence test");
            bool captured = false;
            Action capture = () => {
                captured = true;
                Assert.That(SceneTransitionData.IsMinorRealm, Is.True);
                Assert.That(SceneTransitionData.RealmSeed, Is.EqualTo(123));
                Assert.That(SceneTransitionData.ReturnScene, Is.EqualTo("Universe"));
                throw new StopBeforeUnload();
            };
            SceneTransitionData.BeforeSceneTransition += capture;
            try
            {
                IInteractable portal;
                if (realmPortal) portal = go.AddComponent<RealmPortal>();
                else
                {
                    var normal = go.AddComponent<Portal>();
                    if (exitPortal) normal.SetAsExitPortal();
                    portal = normal;
                }
                Assert.Throws<StopBeforeUnload>(() => portal.Interact(go));
                Assert.That(captured, Is.True);
            }
            finally
            {
                SceneTransitionData.BeforeSceneTransition -= capture;
                SceneTransitionData.ResetAll();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
