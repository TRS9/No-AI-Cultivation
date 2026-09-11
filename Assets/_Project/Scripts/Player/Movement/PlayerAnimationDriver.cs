using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationDriver : MonoBehaviour
    {
        private Animator animator;
        private PlayerMovement movement;
        private PlayerStats stats;
        private HealthSystem health;
        private CapsuleCollider capsule;
        private float previousHealth;
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int LocomotionRate = Animator.StringToHash("LocomotionRate");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            movement = GetComponentInParent<PlayerMovement>();
            stats = GetComponentInParent<PlayerStats>();
            health = GetComponentInParent<HealthSystem>();
            capsule = GetComponentInParent<CapsuleCollider>();
        }

        private void OnEnable()
        {
            GameEvents.OnMeditationToggled += Meditate;
            GameEvents.OnPlayerDied += Die;
            GameEvents.OnPlayerRespawned += Revive;
            GameDataEvents.OnCraftingStarted += StartCraft;
            GameDataEvents.OnCraftingCompleted += EndCraft;
            GameDataEvents.OnCraftingFailed += EndCraft;
            if (health != null)
            {
                previousHealth = health.CurrentHealth;
                health.OnHealthChanged += HealthChanged;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnMeditationToggled -= Meditate;
            GameEvents.OnPlayerDied -= Die;
            GameEvents.OnPlayerRespawned -= Revive;
            GameDataEvents.OnCraftingStarted -= StartCraft;
            GameDataEvents.OnCraftingCompleted -= EndCraft;
            GameDataEvents.OnCraftingFailed -= EndCraft;
            if (health != null) health.OnHealthChanged -= HealthChanged;
        }

        private void Start()
        {
            Meditate(stats != null && stats.isMeditating);
            animator.SetLayerWeight(1, 0);
        }
        private void Update()
        {
            if (movement == null || movement.rb == null) return;
            animator.SetFloat(VerticalSpeed, movement.rb.linearVelocity.y);
            float speed = new Vector2(movement.rb.linearVelocity.x, movement.rb.linearVelocity.z).magnitude;
            animator.SetFloat(LocomotionRate, speed < .05f ? 1 : speed < movement.moveSpeed ? Mathf.Clamp(speed / movement.moveSpeed, .2f, 1) : 1);
            // A null-motion override state retains its last pose. Fade the action
            // layer out completely while locomotion or full-body actions own it.
            var current = animator.GetCurrentAnimatorStateInfo(1);
            var next = animator.GetNextAnimatorStateInfo(1);
            bool action = animator.IsInTransition(1) ? !next.IsName("Empty") : !current.IsName("Empty");
            float target = action && !animator.GetBool("Dead") && !animator.GetBool("Meditating") ? 1 : 0;
            animator.SetLayerWeight(1, Mathf.MoveTowards(animator.GetLayerWeight(1), target, Time.deltaTime * 20));
        }

        private void Meditate(bool value) => animator.SetBool("Meditating", value);
        private void StartCraft(RecipeData recipe) => animator.SetBool("Crafting", true);
        private void EndCraft(RecipeData recipe) => animator.SetBool("Crafting", false);
        private void Die() { animator.SetBool("Dead", true); animator.SetBool("Crafting", false); }
        private void Revive() { animator.SetBool("Dead", false); animator.ResetTrigger("Hurt"); }
        private void HealthChanged(float value, float max)
        {
            if (value < previousHealth && value > 0) animator.SetTrigger("Hurt");
            previousHealth = value;
        }

        private void OnAnimatorIK(int layer)
        {
            if (layer != 0 || movement == null || capsule == null || !movement.IsGrounded() ||
                movement.IsControlBlocked || !animator.GetCurrentAnimatorStateInfo(0).IsName("Movement")) return;
            AdjustFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, animator.leftFeetBottomHeight);
            AdjustFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, animator.rightFeetBottomHeight);
        }

        private void AdjustFoot(AvatarIKGoal goal, HumanBodyBones bone, float soleHeight)
        {
            Transform foot = animator.GetBoneTransform(bone);
            float lift = foot.position.y - capsule.bounds.min.y - soleHeight;
            float weight = Mathf.Clamp01(1 - Mathf.Max(0, lift) / .14f);
            if (!Physics.Raycast(foot.position + Vector3.up * .3f, Vector3.down, out var hit,
                .65f, movement.groundLayer, QueryTriggerInteraction.Ignore)) return;
            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, weight * .65f);
            animator.SetIKPosition(goal, hit.point + Vector3.up * soleHeight);
            animator.SetIKRotation(goal, Quaternion.FromToRotation(Vector3.up, hit.normal) * animator.GetIKRotation(goal));
        }
    }
}
