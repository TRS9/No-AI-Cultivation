using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using CultivationGame.Core;
using CultivationGame.Player;

namespace CultivationGame.Tests
{
    public class PlayerMovementTests
    {
        private GameObject root, floor, camera;
        private PlayerMovement movement;
        private Keyboard keyboard;
        private InputActionAsset actions;
        private InputActionReference move, jump, sprint;
        private PhysicsMaterial movementMaterial;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("TestPlayer");
            var moving = map.AddAction("Move", InputActionType.Value);
            moving.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w");
            move = InputActionReference.Create(moving);
            jump = InputActionReference.Create(map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space"));
            sprint = InputActionReference.Create(map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift"));
            actions.devices = new UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>(new InputDevice[] { keyboard });
            actions.Enable();
            camera = new GameObject("Movement test camera", typeof(Camera)); camera.tag = "MainCamera";
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(60, 1, 60);
            root = new GameObject("Movement test player", typeof(CapsuleCollider), typeof(Rigidbody));
            root.transform.position = new Vector3(0, 1, 0);
            root.GetComponent<CapsuleCollider>().height = 2;
            // Match the scene capsule: motor braking owns ground friction.
            movementMaterial = new PhysicsMaterial("Movement test friction")
            { staticFriction = 0, dynamicFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
            root.GetComponent<CapsuleCollider>().sharedMaterial = movementMaterial;
            var body = root.GetComponent<Rigidbody>(); body.constraints = RigidbodyConstraints.FreezeRotation;
            movement = root.AddComponent<PlayerMovement>(); movement.enabled = false;
            movement.rb = body; movement.move = move; movement.jump = jump; movement.sprint = sprint;
            movement.moveSpeed = 1.8f; movement.sprintSpeed = 4.8f; movement.jumpForce = 6; movement.groundLayer = 1;
            movement.enabled = true;
            yield return new WaitForSeconds(.15f);
            Assert.That(movement.IsGrounded(), Is.True, "Player should start grounded on the test floor.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root); Object.Destroy(floor); Object.Destroy(camera);
            yield return null;
            actions.Disable(); InputSystem.RemoveDevice(keyboard);
            Object.Destroy(move); Object.Destroy(jump); Object.Destroy(sprint); Object.Destroy(actions);
            Object.Destroy(movementMaterial);
        }

        [UnityTest]
        public IEnumerator Jump_HasBoundedHeight_AndSettlesWithoutHorizontalDrift()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            float peak = 1, elapsed = 0;
            while (elapsed < 1.5f)
            {
                yield return new WaitForFixedUpdate(); elapsed += Time.fixedDeltaTime;
                peak = Mathf.Max(peak, root.transform.position.y);
            }
            Assert.That(peak - 1, Is.InRange(1.45f, 1.95f));
            Assert.That(movement.IsGrounded(), Is.True, "Holding Jump must not repeatedly launch the player.");
            Assert.That(root.transform.position.y, Is.EqualTo(1).Within(.06f));
            Assert.That(new Vector2(root.transform.position.x, root.transform.position.z).magnitude, Is.LessThan(.02f));
        }

        [UnityTest]
        public IEnumerator SprintRelease_BrakesWithinPointThreeFiveMetres()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(.5f);
            Assert.That(new Vector2(movement.rb.linearVelocity.x, movement.rb.linearVelocity.z).magnitude, Is.GreaterThan(4.5f),
                $"position={root.transform.position}, velocity={movement.rb.linearVelocity}, ground={movement.IsGrounded()}, move={move.action.ReadValue<Vector2>()}, sprint={sprint.action.IsPressed()}, stamina={movement.currentStamina}, camera={Camera.main.transform.eulerAngles}, blocked={movement.IsControlBlocked}");
            Vector3 releasedAt = root.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(.2f);
            Assert.That(new Vector2(movement.rb.linearVelocity.x, movement.rb.linearVelocity.z).magnitude, Is.LessThan(.05f));
            Assert.That(Vector3.Distance(root.transform.position, releasedAt), Is.LessThan(.35f));
        }

        [UnityTest]
        public IEnumerator UphillSprint_StaysGrounded()
        {
            floor.transform.rotation = Quaternion.Euler(-12, 0, 0);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            int grounded = 0;
            float initialHeight = root.transform.position.y;
            for (int i = 0; i < 50; i++)
            {
                yield return new WaitForFixedUpdate();
                if (movement.IsGrounded()) grounded++;
            }
            Assert.That(root.transform.position.y - initialHeight, Is.GreaterThan(.4f));
            Assert.That(grounded, Is.GreaterThanOrEqualTo(45), "Slope travel must not repeatedly switch to Fall.");
        }

        [UnityTest]
        public IEnumerator MeditationAndDeath_BlockMovementAndJump_UntilBothAreCleared()
        {
            GameEvents.RaiseMeditationToggled(true);
            GameEvents.RaisePlayerDied();
            GameEvents.RaiseMeditationToggled(false);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
            yield return new WaitForSeconds(.3f);
            Assert.That(movement.IsControlBlocked, Is.True);
            Assert.That(root.transform.position.y, Is.EqualTo(1).Within(.05f));
            Assert.That(root.transform.position.z, Is.EqualTo(0).Within(.03f));
            GameEvents.RaisePlayerRespawned();
            Assert.That(movement.IsControlBlocked, Is.False);
        }
    }
}
