using System.Collections.Generic;
using UnityEngine;
using DnDTactics.Characters;
using DnDTactics.Data;

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
            HasCondition(ConditionType.Unconscious);

        // Add a condition (no duplicate stacking of the same type). rounds = -1 means it
        // persists until explicitly removed (Prone, until you stand up).
        public void AddCondition(ConditionType type, int rounds = -1, string source = null)
        {
            if (HasCondition(type)) return;
            conditions.Add(new ActiveCondition(type, rounds, source));
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