using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mousetrap;
using Content.Shared.StepTrigger;

namespace Content.Server.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MousetrapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<MousetrapComponent, StepTriggeredEvent>(OnStepTrigger);
    }

    private void OnUseInHand(EntityUid uid, MousetrapComponent component, UseInHandEvent args)
    {
        component.IsActive = !component.IsActive;

        UpdateVisuals(uid);
    }

    private void OnStepTriggerAttempt(EntityUid uid, MousetrapComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = component.IsActive;
    }

    private void OnStepTrigger(EntityUid uid, MousetrapComponent component, ref StepTriggeredEvent args)
    {
        Trigger(uid, args.Tripper, component);

        UpdateVisuals(uid);
    }

    // This **WAS** its own thing (related to damage on step), but that is NOT how it worked
    // hilariously, it caused the damage to instead stack several times in a row,
    // resulting in damage * however many times it stacked
    private void Trigger(EntityUid uid, EntityUid target, MousetrapComponent? component = null)
    {
        if (!Resolve(uid, ref component)
            || !component.IsActive)
        {
            return;
        }

        var damage = new DamageSpecifier(component.Damage);

        foreach (var slot in component.IgnoreDamageIfSlotFilled)
        {
            if (!_inventorySystem.TryGetSlotContainer(target, slot, out var container, out _))
            {
                continue;
            }

            // This also means that wearing slippers won't
            // hurt the entity.
            if (container.ContainedEntity != null)
            {
                damage = new();
            }
        }

        if (TryComp(target, out PhysicsComponent? physics) && !damage.Empty)
        {
            // The idea here is inverse,
            // Small - big damage,
            // Large - small damage
            // yes i punched numbers into a calculator until the graph looked right
            var scaledDamage = (-50 * Math.Atan(physics.Mass - 10)) + (25 * Math.PI);
            damage *= scaledDamage;
        }

        _damageableSystem.TryChangeDamage(target, damage);
        _triggerSystem.Trigger(uid);

        component.IsActive = false;
    }

    private void UpdateVisuals(EntityUid uid, MousetrapComponent? mousetrap = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref mousetrap, ref appearance, false))
        {
            return;
        }

        appearance.SetData(MousetrapVisuals.Visual,
            mousetrap.IsActive ? MousetrapVisuals.Armed : MousetrapVisuals.Unarmed);
    }
}
