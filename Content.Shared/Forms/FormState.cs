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

public sealed class TestFormState : FormState
{
    [FormField] public string TestField = string.Empty;

    public void Test()
    {
        // This is probably(?) the better option, because it means we
        // can have builtins and user-extendable functions by just
        // using IoC and then adding custom types on system initialization
        // This will require more child classes, so that we can ensure that
        // we have a bound on each registered type and the eventual
        // form state send that has to occur.
        var dict = new Dictionary<Type, Func<string>>
        {
            [typeof(string)] = () => "string"
        };

        foreach (var p in GetType().GetProperties())
        {
            if (!p.TryGetCustomAttribute<FormFieldAttribute>(out _))
            {
                continue;
            }

            if (!dict.TryGetValue(p.PropertyType, out var func))
            {
                continue;
            }

            func();

            // I am going to shit
            switch (p.PropertyType)
            {
                case {} t when t == typeof(string):
                    break;
                case {} t when t == typeof(int):
                    break;
            }
        }
    }
}
