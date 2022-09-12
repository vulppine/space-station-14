using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Content.Client.Forms.UI.Widgets;
using Content.Client.Forms.UI.Windows;
using Content.Shared.Forms;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Reflection;
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
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    /// <summary>
    ///     Default factory passed into all types. Specify a custom FormFactory, if you want
    ///     a custom set of FormFields.
    /// </summary>
    private readonly FormFieldFactory _defaultFactory = FormFieldFactory.Default();
    private readonly Dictionary<Type, FormFactory> _factories = new();
    private readonly Dictionary<Type, Type> _stateWindowBindings = new();

    public FormManager()
    {
        IoCManager.InjectDependencies(this);
        var reflection = IoCManager.Resolve<IReflectionManager>();

        foreach (var type in reflection.GetAllChildren<FormWindow>())
        {
            if (!type.TryGetCustomAttribute<FormDialogAttribute>(out var attr))
            {
                continue;
            }

            _stateWindowBindings.Add(attr.StateType, type);
        }
    }

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

    #region Widget/window factories
    public bool TryGetFactory(Type type, [NotNullWhen(true)] out FormFactory? factory)
    {
        return _factories.TryGetValue(type, out factory);
    }

    /// <summary>
    ///     Gets a new form window based on the given state type. If the form
    ///     state does not have a custom window type, a default window is
    ///     given instead.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>
    ///     Custom window type specified by <see cref="FormDialogAttribute"/>,
    ///     otherwise a default window.
    /// </returns>
    public FormWindow GetWindow(Type type)
    {
        if (!_stateWindowBindings.TryGetValue(type, out var window))
        {
            return new FormWindow();
        }

        return (FormWindow) _typeFactory.CreateInstance(window);
    }
    #endregion
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class FormDialogAttribute : Attribute
{
    public readonly Type StateType;

    public FormDialogAttribute(Type stateType)
    {
        StateType = stateType;
    }
}
