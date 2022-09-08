using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Forms;

[Serializable, NetSerializable]
public enum FormKey
{
    Key
}

// FormState. This is what form UIs send over.
//
// This is an abstract class that should be derived,
// as there's no way to explicitly send a type over network serialization.
//
// This is similar to BoundUserInterfaceState.
//
// Forms will send a form state back to whatever sent it, but as the child
// type, rather than the abstract FormState.
public abstract class FormState
{
}

// Form field. Add this attribute to a form state's properties,
// and you'll get the UI fields required.
public sealed class FormFieldAttribute : Attribute
{}
