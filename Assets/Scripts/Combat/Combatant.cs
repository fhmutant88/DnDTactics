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