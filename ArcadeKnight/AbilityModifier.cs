using ArcadeKnight.Enums;

namespace ArcadeKnight;

public class AbilityModifier : Obstacle
{
    #region Properties

    public string AffectedAbility { get; set; }

    public bool SetValue { get; set; }

    public CheckDirection RevertDirection { get; set; }

    #endregion
}
