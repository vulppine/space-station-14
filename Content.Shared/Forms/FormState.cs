using System.Net.Mime;
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
[Serializable, NetSerializable]
public abstract class FormState
{
}

// Form field. Add this attribute to a form state's properties,
// and you'll get the UI fields required.
public sealed class FormFieldAttribute : Attribute
{}

/// <summary>
///     Text dialog form state.
/// </summary>
[Serializable, NetSerializable]
public sealed class TextDialogFormState : FormState
{
    /// <summary>
    ///     The text in this dialog.
    /// </summary>
    public string Text = string.Empty;
}

/// <summary>
///     FormType. This gives the client more information on what to do
///     with the form state it receives. A dialog-type form will always
///     try to open a window with the form embedded into it, while an
///     embedded form will not try to open a window. Having both flags
///     is a valid state.
/// </summary>
[Flags]
[Serializable, NetSerializable]
public enum FormType
{
    /// <summary>
    ///     Embedded into another UI.
    /// </summary>
    Embedded,
    /// <summary>
    ///     This opens up as its own UI/dialog window.
    /// </summary>
    Dialog
}
