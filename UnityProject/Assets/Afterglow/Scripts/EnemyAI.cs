using System.Collections.Generic;
using UnityEngine;

namespace Afterglow
{
    public enum EnemyAIState { Patrol, Alert, Combat, Search }

    /// <summary>Four-state sentry brain with sight, sound investigation and last-known-position search.</summary>
    public sealed class EnemyAI : MonoBehaviour
    {
        private static readonly List<EnemyAI> Active = new List<EnemyAI>();
        private AfterglowEnemy enemy;
        private AfterglowPlayer player;
        private Vector2 lastKnownPlayer;
        private float homeX;
        private float stateTimer;
        private float lostSightTimer;
        private float patrolDirection = 1f;
        private float searchTurnTimer;

        public EnemyAIState State { get; private set; } = EnemyAIState.Patrol;

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable() => Active.Remove(this);

        public void Configure(AfterglowEnemy host, AfterglowPlayer target)
        {
            enemy = host;
            player = target;
            homeX = transform.position.x;
            State = EnemyAIState.Patrol;
        }

        public static void AlertNearby(Vector2 source, float radius, AfterglowPlayer instigator)
        {
            float radiusSquared = radius * radius;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                EnemyAI brain = Active[i];
                if (brain == null) { Active.RemoveAt(i); continue; }
                if (brain.enemy == null || brain.enemy.IsDead || brain.player == null) continue;
                if (((Vector2)brain.transform.position - source).sqrMagnitude <= radiusSquared)
                    brain.Hear(source, instigator);
            }
        }

        public void ReactToPlayerAttack(AfterglowPlayer attacker)
        {
            if (attacker == null) return;
            player = attacker;
            Hear(attacker.transform.position, attacker);
            Transition(EnemyAIState.Combat, 0f);
        }

        private void Hear(Vector2 source, AfterglowPlayer instigator)
        {
            if (instigator != null) player = instigator;
            lastKnownPlayer = source;
            if (State == EnemyAIState.Patrol || State == EnemyAIState.Search)
                Transition(EnemyAIState.Alert, 1.05f);
            else if (State == EnemyAIState.Alert)
                stateTimer = Mathf.Max(stateTimer, .7f);
        }

        private void Update()
        {
            if (enemy == null || enemy.IsDead || player == null || player.IsDead || AfterglowUI.IsPaused || enemy.IsLaunched)
                return;
            if (!player.HasStartedGameplay)
            {
                SetHorizontalSpeed(0f, 24f);
                return;
            }

            float dt = Time.deltaTime;
            bool seesPlayer = HasLineOfSight();
            if (seesPlayer)
            {
                lastKnownPlayer = player.transform.position;
                lostSightTimer = 0f;
            }
            else lostSightTimer += dt;

            stateTimer -= dt;
            switch (State)
            {
                case EnemyAIState.Patrol: TickPatrol(seesPlayer, dt); break;
                case EnemyAIState.Alert: TickAlert(seesPlayer, dt); break;
                case EnemyAIState.Combat: TickCombat(seesPlayer, dt); break;
                case EnemyAIState.Search: TickSearch(seesPlayer, dt); break;
            }
        }

        private bool HasLineOfSight()
        {
            if (player == null) return false;
            Vector2 delta = player.transform.position - transform.position;
            float notice = enemy.Kind == EnemyKind.HexCaster ? 13f : 10f;
            return Mathf.Abs(delta.x) <= notice && Mathf.Abs(delta.y) <= 5.5f;
        }

        private void TickPatrol(bool seesPlayer, float dt)
        {
            if (seesPlayer)
            {
                Transition(EnemyAIState.Alert, .35f);
                return;
            }
            if (transform.position.x < homeX - 3.4f) patrolDirection = 1f;
            else if (transform.position.x > homeX + 3.4f) patrolDirection = -1f;
            else if (Random.value < dt * .12f) patrolDirection *= -1f;
            SetFacing(patrolDirection < 0f);
            SetHorizontalSpeed(patrolDirection * PatrolSpeed(), 22f);
        }

        private void TickAlert(bool seesPlayer, float dt)
        {
            if (seesPlayer && stateTimer <= 0f)
            {
                Transition(EnemyAIState.Combat, 0f);
                return;
            }
            Vector2 destination = seesPlayer ? (Vector2)player.transform.position : lastKnownPlayer;
            float dx = destination.x - transform.position.x;
            SetFacing(dx < 0f);
            SetHorizontalSpeed(Mathf.Abs(dx) > 1.2f ? Mathf.Sign(dx) * PatrolSpeed() * .8f : 0f, 18f);
            if (!seesPlayer && Mathf.Abs(dx) < .65f && stateTimer <= 0f)
                Transition(EnemyAIState.Search, 2.4f);
        }

        private void TickCombat(bool seesPlayer, float dt)
        {
            if (!seesPlayer)
            {
                if (lostSightTimer > 1.25f) Transition(EnemyAIState.Search, 2.8f);
                SetHorizontalSpeed(0f, 20f);
                return;
            }

            Vector2 delta = player.transform.position - transform.position;
            float distance = Mathf.Abs(delta.x);
            float toward = Mathf.Sign(delta.x == 0f ? 1f : delta.x);
            SetFacing(toward < 0f);
            float desiredSpeed = enemy.Kind == EnemyKind.HexCaster
                ? (distance < 3.4f ? -toward * PatrolSpeed() : distance > 5.4f ? toward * PatrolSpeed() : 0f)
                : distance > PreferredDistance() ? toward * PatrolSpeed() : distance < PreferredDistance() * .55f ? -toward * PatrolSpeed() * .65f : 0f;
            SetHorizontalSpeed(desiredSpeed, 24f);
            enemy.TryAttack(distance, toward);
        }

        private void TickSearch(bool seesPlayer, float dt)
        {
            if (seesPlayer)
            {
                Transition(EnemyAIState.Combat, 0f);
                return;
            }
            float dx = lastKnownPlayer.x - transform.position.x;
            SetHorizontalSpeed(Mathf.Abs(dx) > .8f ? Mathf.Sign(dx) * PatrolSpeed() * .72f : 0f, 16f);
            searchTurnTimer -= dt;
            if (searchTurnTimer <= 0f)
            {
                searchTurnTimer = .55f;
                SetFacing(Random.value < .5f);
            }
            if (stateTimer <= 0f) Transition(EnemyAIState.Patrol, 0f);
        }

        private float PatrolSpeed() => enemy.Kind == EnemyKind.Breaker ? 1.35f : enemy.Kind == EnemyKind.RiftHound ? 3.5f : enemy.Kind == EnemyKind.HexCaster ? 2f : 2.2f;
        private float PreferredDistance() => enemy.Kind == EnemyKind.Breaker ? 2.2f : enemy.Kind == EnemyKind.RiftHound ? 1.4f : 2.5f;

        private void SetHorizontalSpeed(float speed, float acceleration)
        {
            Rigidbody2D body = enemy.Body;
            if (body == null) return;
            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, speed, acceleration * Time.deltaTime);
            body.linearVelocity = velocity;
        }

        private void SetFacing(bool left) => enemy.SetFacing(left);

        private void Transition(EnemyAIState next, float duration)
        {
            State = next;
            stateTimer = duration;
            if (next == EnemyAIState.Search) searchTurnTimer = 0f;
            if (next == EnemyAIState.Combat) lostSightTimer = 0f;
        }
    }
}
