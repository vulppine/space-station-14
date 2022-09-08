using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Content.Shared.Forms;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using TerraFX.Interop.Windows;

// Forms. Either generated (OOP-y, a little ass) or compositional (based on Control namespaces).
// Generated forms are meant to ease the boilerplate that arises from
// dealing with simple dialog boxes (having to do repetitive BUI states/EUI instances isn't fun),
//
// and compositional forms are meant to ease having to fetch and match information that
// some set of dialog boxes can hold. It's meant to make it so that you can compose them
// in XAML.

namespace Content.Client.Forms;

// am I allowed to emit IL in content??????????????????????????????
// i am using SO MUCH reflection here

public sealed class FormFieldFactory : Dictionary<Type, Func<IFormField>>
{
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

    public FormFieldFactory()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Add<T>(Type type) where T : IFormField, new()
    {
        if (ContainsKey(type))
        {
            return;
        }

        // We use the IDynamicTypeFactory method to create our instance.
        Add(type, () => _typeFactory.CreateInstance<T>());
    }

    public IFormField GenerateField(Type fieldType, object? initialValue)
    {
        if (!TryGetValue(fieldType, out var factory))
        {
            throw new ArgumentException($"Could not find type of {fieldType}.");
        }

        var field = factory();
        field.Value = initialValue ?? field.Value;

        return field;
    }

    /// <summary>
    ///     The default set of form field widgets.
    /// </summary>
    public static FormFieldFactory Default = new()
    {
        [typeof(string)] = () => new TextFormField()
    };
}

/// <summary>
///     This is the object that creates a form.
/// </summary>
public sealed class FormFactory
{
    /// <summary>
    ///     This is the object that creates form fields.
    /// </summary>
    private FormFieldFactory _fieldFactory = new();

    /// <summary>
    ///     This is what creates a form model.
    /// </summary>
    private readonly Func<FormModel> _formModel;

    public FormModel GetModel()
    {
        return _formModel();
    }

    public FormFactory(Type type, FormFieldFactory fieldFactory)
    {
        _formModel = () => new FormModel(type);
        _fieldFactory = fieldFactory;
    }

    public FormFactory(Type type) : this(type, FormFieldFactory.Default)
    {
    }
}

/// <summary>
///     Deals with creating forms.
/// </summary>
public sealed class FormManager
{
    private readonly Dictionary<Type, FormFactory> _factories = new();

    /// <summary>
    ///     Register a type, giving it a custom form factory.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="factory"></param>
    public void RegisterType(Type type, FormFactory factory)
    {
        _factories.Add(type, factory);
    }

    /// <summary>
    ///     Register a type, giving it the default FormFactory.
    /// </summary>
    /// <param name="type"></param>
    public void RegisterType(Type type)
    {
        RegisterType(type, new FormFactory(type));
    }
}

public sealed class FormDialogAttribute : Attribute
{
    public Type StateType;

    public FormDialogAttribute(Type stateType)
    {
        StateType = stateType;
    }
}

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
}

[FormDialog(typeof(TextDialogFormState))]
public sealed class TextDialogFormWindow : FormWindow
{}
