using System.Reflection;
using Content.Shared.Forms;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Forms;

public sealed class FormModel
{
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    /*
    /// <summary>
    ///     The current state in the form model.
    ///     I honestly don't like this, but if
    ///     this is wrapped around a sane API,
    ///     then who cares?
    /// </summary>
    private readonly object _state;
    */

    /// <summary>
    ///     The type that this form model represents.
    /// </summary>
    public readonly Type Type;

    public bool IsFormState => Type.IsAssignableFrom(typeof(FormState));

    /// <summary>
    ///     Fields that this form model manipulates.
    /// </summary>
    private readonly Dictionary<string, IFormField> _inputs = new();

    /// <summary>
    ///     Creates a form model.
    /// </summary>
    public FormModel(Type type)
    {
        IoCManager.InjectDependencies(this);
        Type = type;
    }

    #region Field registration
    /// <summary>
    ///     Register a single IFormField by name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public bool RegisterField(string name, IFormField field)
    {
        field.FieldName = name;
        return _inputs.TryAdd(name, field);
    }

    /// <summary>
    ///     Recurse through controls to register all controls that
    ///     are also IFormField.
    /// </summary>
    /// <param name="root"></param>
    public void RegisterControls(Control root)
    {
        if (root is IFormField rootFormField && !string.IsNullOrEmpty(root.Name))
        {
            RegisterField(root.Name, rootFormField);
        }

        foreach (var control in root.Children)
        {
            if (control is IFormField controlFormField && !string.IsNullOrEmpty(control.Name))
            {
                RegisterField(control.Name, controlFormField);
            }

            RegisterControls(root);
        }
    }
    #endregion

    #region State setters/getters
    /// <summary>
    ///     Set the state of this form model.
    /// </summary>
    public void SetState(object state)
    {
        var instanceType = state.GetType();
        if (instanceType != Type)
        {
            throw new Exception("Invalid type for this form model.");
        }

        foreach (var member in GetFieldsPropertiesValues(state))
        {
            if (member.Value == null
                || !_inputs.TryGetValue(member.Name, out var field)
                || field.Value.GetType() != member.Type)
            {
                // TODO: Could this be an exception too?
                continue;
            }

            field.Value = member.Value;
        }
    }

    // TODO: Could this be represented by the visitor pattern?
    /// <summary>
    ///     Gets the state of the form model. Updates the state,
    ///     then returns it.
    /// </summary>
    /// <returns></returns>
    public object GetState()
    {
        // THIS IS VALID, THOUGH >:3
        var state = _typeFactory.CreateInstance(Type);
        if (state == null)
        {
            throw new Exception("Could not get instance of type.");
        }

        foreach (var member in GetFieldsProperties())
        {
            if (!_inputs.TryGetValue(member.Name, out var field))
            {
                // TODO: Should this be an exception?
                continue;
            }

            if (!SetFieldPropertyValue(state, member, field.Value))
            {
                // TODO: This should be an exception.
                continue;
            }
        }

        return state;
    }

    /// <summary>
    ///     Gets the state of the form model as type T. Updates the state,
    ///     then returns it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetState<T>()
    {
        if (typeof(T) != Type)
        {
            throw new Exception("Invalid type for this form model.");
        }

        return (T) GetState();
    }
    #endregion

    #region Private API
    private bool SetFieldPropertyValue(object state, MemberInfo member, object value)
    {
        var valueType = value.GetType();
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                var memberField = Type.GetField(member.Name)!;
                if (memberField.FieldType != valueType)
                {
                    return false;
                }

                memberField.SetValue(state, value);

                break;
            case MemberTypes.Property:
                var memberProperty = Type.GetProperty(member.Name)!;
                if (memberProperty.PropertyType != valueType || !memberProperty.CanWrite)
                {
                    return false;
                }

                memberProperty.SetValue(state, value);
                break;
        }

        return true;
    }

    private List<MemberInfo> GetFieldsProperties()
    {
        var result = new List<MemberInfo>();
        foreach (var member in Type.GetMembers())
        {
            if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
            {
                continue;
            }

            if (!IsFormState && !member.HasCustomAttribute<FormFieldAttribute>())
            {
                continue;
            }

            result.Add(member);
        }

        return result;
    }

    private static List<(Type Type, string Name, object? Value)> GetFieldsPropertiesValues(object obj)
    {
        var result = new List<(Type, string, object?)>();
        var isFormState = obj is FormState;
        var type = obj.GetType();
        foreach (var member in type.GetMembers())
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
            object? value;
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var memberField = type.GetField(member.Name)!;
                    memberType = memberField.FieldType;
                    value = memberField.GetValue(obj);

                    break;
                case MemberTypes.Property:
                    var memberProperty = type.GetProperty(member.Name)!;
                    memberType = memberProperty.PropertyType;
                    value = memberProperty.GetValue(obj);

                    break;
                default:
                    throw new Exception("Reached invalid state.");
            }

            if (value == null)
            {
                continue;
            }

            result.Add((memberType, name, value));
        }

        return result;
    }

    #endregion
}

// Forms, as MVC:
//
// The abstract form object should be our 'model'.
//
// The controller, which receives the form's state and transmits it to the server,
// must manipulate the form object somehow.
// The model, when updated or manipulated, should update any related fields in
//
// the UI.

// The controller should store the form model, or a reference to it.
// Controllers can either be the one that auto-generates the form, or
// a custom implementation written using BUI/EUI. Either way, the form model
// has to be stored somewhere.
//
// The form model itself should be separated from the actual data object.
// It's there to process fields, and inject values where needed.
//
// Form models should have these public APIs:
// - SetState, which sets the state for a form model.
// - SetValue, which sets the value for a form model's field.
// Whenever the state of a form model is changed, it should tell the view to update.

// Forms as MVC ends up looking like this:
// Controller gets update (either through server, or through the user modifying information) ->
// Model receives update (does callback to widgets in view, updates their values) ->
// View presents update

// On UI init, the controller stores a copy of the form model state.
// The controller either auto-generates a form model and widgets based on the base type,
// or an implementation registers widgets to the correct fields on the form model.
// Either way, something must register the widgets in the view to the form model.
//
// Upon updating the model, the model should perform callbacks to the widgets, giving
// the widgets their new values.
// The controller is in charge of dealing with server updates.
