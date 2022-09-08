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

    public IFormField GenerateField(Type fieldType)
    {
        if (!TryGetValue(fieldType, out var factory))
        {
            throw new ArgumentException($"Could not find type of {fieldType}.");
        }

        var field = factory();

        return field;
    }

    public bool TryGenerateField(Type fieldType, [NotNullWhen(true)] out IFormField? field)
    {
        field = null;
        if (!TryGetValue(fieldType, out var factory))
        {
            return false;
        }

        field = factory();

        return true;
    }

    /// <summary>
    ///     The default set of form field widgets.
    /// </summary>
    public static FormFieldFactory Default()
    {
        return new()
        {
            [typeof(string)] = () => new TextFormField()
        };
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

    private Type _type;

    /// <summary>
    ///     Creates a new instance of a form model with the type backing this FormFactory.
    ///     Use this to insert a model into your controller object.
    /// </summary>
    /// <returns></returns>
    public FormModel GetModel()
    {
        return new FormModel(_type);
    }

    /// <summary>
    ///     Creates a new set of widgets using the type backing this FormFactory.
    ///     Use these to auto-generate any views the user may see.
    /// </summary>
    /// <returns></returns>
    public List<IFormField> GetWidgets()
    {
        var result = new List<IFormField>();
        foreach (var field in GetFieldsProperties())
        {
            if (!_fieldFactory.TryGenerateField(field.Type, out var widget))
            {
                continue;
            }

            widget.FieldName = field.Name;
            result.Add(widget);
        }

        return result;
    }

    public FormFactory(Type type, FormFieldFactory fieldFactory)
    {
        _type = type;
        _fieldFactory = fieldFactory;
    }

    public FormFactory(Type type) : this(type, FormFieldFactory.Default())
    {
    }

    private List<(Type Type, string Name)> GetFieldsProperties()
    {
        var result = new List<(Type, string)>();
        var isFormState = _type.IsAssignableFrom(typeof(FormState));

        foreach (var member in _type.GetMembers())
        {
            if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
            {
                continue;
            }

            if (!isFormState && !member.HasCustomAttribute<FormFieldAttribute>())
            {
                continue;
            }

            var name = member.Name;
            Type memberType;
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var memberField = _type.GetField(member.Name)!;
                    memberType = memberField.FieldType;

                    break;
                case MemberTypes.Property:
                    var memberProperty = _type.GetProperty(member.Name)!;
                    memberType = memberProperty.PropertyType;

                    break;
                default:
                    throw new Exception("Reached invalid state.");
            }

            result.Add((memberType, name));
        }

        return result;
    }
}

/// <summary>
///     Deals with creating forms.
/// </summary>
public sealed class FormManager
{
    /// <summary>
    ///     Default factory passed into all types. Specify a custom FormFactory, if you want
    ///     a custom set of FormFields.
    /// </summary>
    private readonly FormFieldFactory _defaultFactory = FormFieldFactory.Default();
    private readonly Dictionary<Type, FormFactory> _factories = new();

    #region Registration
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
    ///     Register a type, giving it the default set of widgets registered with this FormManager.
    /// </summary>
    /// <param name="type"></param>
    public void RegisterType(Type type)
    {
        RegisterType(type, new FormFactory(type, _defaultFactory));
    }

    /// <summary>
    ///     Register a widget constructor for a certain type to the set of default widgets.
    /// </summary>
    /// <param name="type"></param>
    /// <typeparam name="T"></typeparam>
    public void RegisterWidget<T>(Type type) where T : IFormField, new()
    {
        _defaultFactory.Add<T>(type);
    }
    #endregion

    public bool TryGetFactory(Type type, [NotNullWhen(true)] out FormFactory? factory)
    {
        return _factories.TryGetValue(type, out factory);
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
