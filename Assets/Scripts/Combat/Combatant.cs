using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Characters;
using DnDTactics.Data;
using DnDTactics.Rules;

namespace DnDTactics.Combat
{
    // A character's presence in a battle. Wraps the persistent Character and adds
    // combat-only state (grid position now; initiative and conditions later).
    public class Combatant : MonoBehaviour
    {
        public Character Character { get; private set; }
        public Team Team { get; private set; }
        public GridCoord Coord { get; private set; }

        public Weapon Weapon { get; private set; }
        public void SetWeapon(Weapon weapon) => Weapon = weapon;

        // If this combatant is a deployed party hero, the id of its BarracksMember.
        // Null/empty for monsters and test combatants.
        public string BarracksMemberId { get; private set; }
        public void SetBarracksMemberId(string id) => BarracksMemberId = id;

        // If this combatant is a monster, the XP awarded for defeating it.
        public int XpReward { get; private set; }
        public void SetXpReward(int xp) => XpReward = xp;

        // If this combatant is a monster with a signature on-hit ability, it lives here (set at spawn).
        // Null for heroes and plain-attacker monsters.
        public DnDTactics.Data.MonsterAbility OnHitAbility { get; private set; }
        public void SetOnHitAbility(DnDTactics.Data.MonsterAbility ability) => OnHitAbility = ability;

        // --- Action economy: reaction ---
        // A reaction is spent during OTHER creatures' turns (e.g. opportunity attacks),
        // so unlike action/bonus/movement it lives here on the combatant, not in
        // TurnResources. Reset at the START of this combatant's own turn.
        public bool ReactionAvailable { get; private set; } = true;
        public void ResetReaction() => ReactionAvailable = true;
        public bool TrySpendReaction()
        {
            if (!ReactionAvailable) return false;
            ReactionAvailable = false;
            return true;
        }

        // Set when this combatant takes the Disengage action; suppresses opportunity attacks
        // against it for the rest of its turn. Reset at the start of its own turn.
        public bool DisengagedThisTurn { get; private set; }
        public void SetDisengaged(bool v) => DisengagedThisTurn = v;

        // --- Conditions (combat-only state) ---
        // A typed list of what's active. Consuming systems (AddAttackModifiers, later movement
        // and saves) query HasCondition and apply the rule locally — conditions are DATA, not
        // behavior, because their effects are scattered across systems. Generalizes the
        // "list of active conditions, each with its own clear rule + effect" pattern.
        private readonly List<ActiveCondition> conditions = new();
        public IReadOnlyList<ActiveCondition> Conditions => conditions;

        public bool HasCondition(ConditionType type)
        {
            foreach (var c in conditions) if (c.Type == type) return true;
            return false;
        }

        // Incapacitated by any condition that removes actions (Paralyzed, later Stunned/Unconscious).
        // Consulted at the start of a turn to skip a creature that can't act.
        public bool IsIncapacitated =>
            HasCondition(ConditionType.Paralyzed) ||
            HasCondition(ConditionType.Stunned) ||
            HasCondition(ConditionType.Unconscious) ||
            HasCondition(ConditionType.Petrified);

        // Add a condition (no duplicate stacking of the same type). rounds = -1 means it
        // persists until explicitly removed (Prone, until you stand up).
        // Add a condition (no duplicate stacking of the same type).
        public void AddCondition(ConditionType type, ClearRule clear = ClearRule.UntilRemoved,
                                 int rounds = -1, string source = null,
                                 Ability saveAbility = Ability.Constitution, int saveDC = 10,
                                 Combatant sourceCombatant = null)
        {
            if (HasCondition(type)) return;
            var cond = new ActiveCondition(type, clear, rounds, source, saveAbility, saveDC);
            cond.SourceCombatant = sourceCombatant;
            conditions.Add(cond);
        }

        // Remove any conditions applied by a specific combatant (e.g. a dead grappler's grapples).
        public void RemoveConditionsFromSource(Combatant source)
        {
            for (int i = conditions.Count - 1; i >= 0; i--)
                if (conditions[i].SourceCombatant == source)
                    conditions.RemoveAt(i);
        }

        public bool RemoveCondition(ConditionType type)
        {
            for (int i = conditions.Count - 1; i >= 0; i--)
                if (conditions[i].Type == type) { conditions.RemoveAt(i); return true; }
            return false;
        }

        // Tick durationed conditions (call at the start of the combatant's turn); drop expired.
        // Prone (-1) is unaffected. Makes future durationed conditions work with no extra wiring.
        public void TickConditions()
        {
            for (int i = conditions.Count - 1; i >= 0; i--)
            {
                conditions[i].Tick();
                if (conditions[i].IsExpired) conditions.RemoveAt(i);
            }
        }

        // End-of-turn escape saves: for each RepeatingSave condition, re-roll; on success, the
        // condition ends. Returns the list of (condition, saveResult) for the caller to log.
        // Called at the affected creature's turn (even though a paralyzed creature is skipped —
        // the save still happens). Removal here is why Ghoul paralysis isn't permanent.
        public System.Collections.Generic.List<(ConditionType type, bool escaped)> ProcessEscapeSaves()
        {
            var outcomes = new System.Collections.Generic.List<(ConditionType, bool)>();
            for (int i = conditions.Count - 1; i >= 0; i--)
            {
                var cond = conditions[i];
                if (cond.Clear != ClearRule.RepeatingSave) continue;

                var result = SaveResolver.Resolve(this, cond.SaveAbility, cond.SaveDC);
                bool escaped = result.success;   // auto-fail (still incapacitated) returns false
                outcomes.Add((cond.Type, escaped));
                if (escaped) conditions.RemoveAt(i);
            }
            return outcomes;
        }

        // --- Death saves (combat-only; resolves to barracks Down/Dead at encounter end) ---
        // A downed hero lingers DYING on the field, rolling a save each turn, until stabilized
        // (3 successes), dead (3 failures), or revived. Combat-only, like conditions — never
        // touches the persistent Character/barracks until the encounter resolves.
        public DeathSaves Death { get; } = new DeathSaves();

        private Renderer bodyRenderer;
        private Color baseColor;
        private MaterialPropertyBlock mpb;

        public void Initialize(Character character, Team team, GridCoord coord, Color color)
        {
            Character = character;
            Team = team;
            Coord = coord;
            baseColor = color;

            bodyRenderer = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            SetColor(baseColor);
        }

        public void SetCoord(GridCoord coord, TacticalGrid grid, float yOffset)
        {
            Coord = coord;
            transform.position = grid.CoordToWorld(coord) + Vector3.up * yOffset;
        }

        public void SetSelected(bool selected) =>
            SetColor(selected ? Color.Lerp(baseColor, Color.white, 0.6f) : baseColor);

        private void SetColor(Color c)
        {
            if (bodyRenderer == null) return;
            bodyRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);   // URP Lit color property
            bodyRenderer.SetPropertyBlock(mpb);
        }
    }
}