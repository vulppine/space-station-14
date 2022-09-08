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

/*
// I SIT HERE IN MY OBJECT VOID
// HOPING THAT STRICT TYPING
// WILL SOMEDAY MAKE ITSELF KNOWN

/// <summary>
///     This is what stores all the form-related construction stuff.
///     Obviously, this can only be client-side. This is mostly
///     used in conjunction with GenericForm.
/// </summary>
public sealed class FormManager
{
    private FormFieldFactory _fieldFactory = new();

    /// <summary>
    ///     Generates a set of FormFields from an object.
    ///     If the object is of type FormState, this will generate all fields/properties.
    ///     Otherwise, it will only generate fields where the field/property has
    ///     <see cref="FormFieldAttribute"/>.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public Dictionary<string, FormField> GenerateFields(object state)
    {
        var result = new Dictionary<string, FormField>();
        foreach (var member in GetFieldsPropertiesValues(state))
        {
            var newField = _fieldFactory.GenerateField(member.Type, member.Value);

            result.Add(member.Name, newField);
        }

        return result;
    }

    /// <summary>
    ///     Updates a form state by directly accessing the control's members.
    ///
    ///     This is more useful for when you have a custom layout for your
    ///     control (e.g., through XAML) and you still want to use
    ///     FormStates in order to pass information to the client.
    /// </summary>
    /// <param name="root">Root control that contains all the form elements we're looking for.</param>
    /// <param name="state"></param>
    public static void UpdateFormState(Control root, object state)
    {
        foreach (var member in GetFieldsPropertiesValues(state))
        {
            var control = root.NameScope?.Find(member.Name);
            if (control is not FormField field)
            {
                continue;
            }

            field.Value = member.Value ?? field.Value;
        }
    }

    /// <summary>
    ///     Get a form's state by checking a control root, and then
    ///     injecting any relevant values into the passed in object.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="state"></param>
    public static void GetFormState(Control root, object state)
    {
        var instanceType = state.GetType();
        foreach (var member in GetFieldsProperties(state))
        {
            var control = root.NameScope?.Find(member.Name);
            if (control is not FormField field)
            {
                continue;
            }

            SetFieldPropertyValue(instanceType, member, field.Value, state);
        }
    }

    /// <summary>
    ///     Updates a form's state by using IForm methods.
    /// </summary>
    /// <param name="form"></param>
    /// <param name="state"></param>
    /// <exception cref="Exception"></exception>
    public static void UpdateFormState(IForm form, object state)
    {
        var instanceType = state.GetType();
        if (instanceType != form.Type)
        {
            throw new Exception($"Cannot update form state from an object of type {instanceType}.");
        }

        foreach (var member in GetFieldsPropertiesValues(state))
        {
            if (member.Value == null)
            {
                continue;
            }

            form.SetFieldValue(member.Name, member.Value);
        }
    }

    /// <summary>
    ///     Gets a form's state by grabbing all the relevant fields,
    ///     and injecting the fields with the correct values.
    /// </summary>
    /// <param name="form"></param>
    /// <param name="state"></param>
    /// <exception cref="Exception"></exception>
    public static void GetFormState(IForm form, object state)
    {
        var stateType = state.GetType();
        if (stateType != form.Type)
        {
            throw new Exception("Form type does not match type specified.");
        }

        foreach (var member in GetFieldsProperties(state))
        {
            if (!form.TryGetFieldValue(member.Name, out var fieldValue))
            {
                continue;
            }

            SetFieldPropertyValue(stateType, member, fieldValue, state);
        }
    }

    private static bool SetFieldPropertyValue(Type type, MemberInfo member, object value, object instance)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                var memberField = type.GetField(member.Name)!;
                memberField.SetValue(instance, value);

                break;
            case MemberTypes.Property:
                var memberProperty = type.GetProperty(member.Name)!;
                if (!memberProperty.CanWrite)
                {
                    return false;
                }

                memberProperty.SetValue(instance, value);
                break;
        }

        return true;
    }

    private static List<MemberInfo> GetFieldsProperties(object obj)
    {
        var result = new List<MemberInfo>();
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

            result.Add(member);
        }

        return result;
    }

    private static List<(Type Type, string Name, object? Value)> GetFieldsPropertiesValues(object obj)
    {
        var result = new List<(Type, string, object?)>();
        var type = obj.GetType();
        foreach (var member in GetFieldsProperties(obj))
        {
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
}
*/

/// <summary>
///     Abstract form.
/// </summary>
public interface IForm
{
    /// <summary>
    ///     The type that this form represents.
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    ///     Set of fields. The keys are fields/properties of the
    ///     form state, and the values are FormField objects that
    ///     *should* return an object correctly tied to the state's
    ///     field/property at that name.
    /// </summary>
    public Dictionary<string, FormField> Fields { get; set; }

    /// <summary>
    ///     Tries to get a field value by name and type.
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="fieldType"></param>
    /// <param name="fieldValue"></param>
    /// <returns></returns>
    public bool TryGetFieldValue(string fieldName, [NotNullWhen(true)] out object? fieldValue)
    {
        fieldValue = null;
        if (!Fields.TryGetValue(fieldName, out var field))
        {
            return false;
        }

        fieldValue = field.Value;

        return true;
    }

    public void SetFieldValue(string name, object value)
    {
        Fields[name].Value = value;
    }

    public void UpdateState(object state);
}

public abstract class EmbeddedForm : BoxContainer, IForm
{
    public Dictionary<string, FormField> Fields { get; set; } = new();
    public abstract Type Type { get; init; }

    public abstract void UpdateState(object state);
}

public abstract class DialogForm : DefaultWindow, IForm
{
    public abstract Type Type { get; init; }
    public Dictionary<string, FormField> Fields { get; set; } = new();
    public void UpdateState(object state)
    {
        throw new NotImplementedException();
    }
}

public sealed class GenericForm : EmbeddedForm
{
    /// <summary>
    ///     Build a form based off of a form state. Uses FormManager in order
    ///     to get all the correct parts to insert into this form.
    /// </summary>
    /// <param name="state">State to use.</param>
    public GenericForm(object state, FormManager? formManager = null)
    {
        IoCManager.Resolve(ref formManager);
        Type = state.GetType();
        Fields = formManager.GenerateFields(state);
        foreach (var field in Fields.Values)
        {
            AddChild(field);
            field.Initialized = true;
        }
    }

    public override void UpdateState(object state)
    {
        FormManager.UpdateFormState((IForm) this, state);
    }

    public override Type Type { get; init; }
}





/// <summary>
///     General 'form field'.
/// </summary>
public abstract class FormField : Control
{
    /// <summary>
    ///     Called when this field is set.
    /// </summary>
    public Action? OnFieldSet;

    public bool Initialized;

    /// <summary>
    ///     The value this field holds.
    /// </summary>
    public abstract object Value { get; set; }
}
