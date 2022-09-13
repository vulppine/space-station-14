using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Forms.UI.Windows;

/// <summary>
///     Form window. By default, this is the parent class of all window types.
///     Children of FormWindow can inherit this, and will become a valid
///     window that can be instianted by FormManager. However, they must specify
///     an attribute, [FormDialog(typeof(T))], so that the form manager knows that
///     this window type is for this form dialog type.
/// </summary>
[Virtual]
public class FormWindow : DefaultWindow
{
    public virtual void SetLabels(IReadOnlyDictionary<string, string> labels)
    {
        // Default implementation (this) doesn't require this. Inheritors can instead
        // implement this based on their own needs.
    }
}
