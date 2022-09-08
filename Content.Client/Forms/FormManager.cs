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

    private readonly Func<FormModel> _formModel;

    public FormModel GetModel()
    {
        return _formModel();
    }

    public FormFactory(Type type, IDynamicTypeFactory? typeFactory = null)
    {
        IoCManager.Resolve(ref typeFactory);

        _formModel = () => (FormModel) typeFactory.CreateInstance(type);
    }
}

/// <summary>
///     Deals with creating forms.
/// </summary>
public sealed class FormManager
{
    /// <summary>
    ///     Set of factories that generate forms based off of
    ///     an enum key. The enum key is the important part,
    ///     as almost all UIs pass in some kind of key.
    /// </summary>
    public Dictionary<Enum, FormFactory> _factories = new()
    {
        // We add in the generic form types here.
        [FormKey.Key] = new()
    };

    /// <summary>
    ///     Register a new form factory to this enum.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    public void Register(Enum key, FormFactory factory)
    {}
}
