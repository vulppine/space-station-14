using Content.Shared.Damage;

namespace Content.Server.Mousetrap;

[RegisterComponent]
public sealed class MousetrapComponent : Component
{
    [ViewVariables]
    public bool IsActive;

    [DataField("damage")]
    public DamageSpecifier Damage = new();

    [DataField("ignoreDamageIfInventorySlotsFilled")]
    public List<string> IgnoreDamageIfSlotFilled = new();
}
