using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Content.Shared.Forms;
using NUnit.Framework;
using Robust.Shared.Utility;
using SharpFont;

namespace Content.Tests.Shared;

/*
[TestFixture]
public sealed class FormStateTest
{
    private Dictionary<Type, Func<object, bool>> ValidTypesAtInit = new()
    {
        // This has a default value of test.
        [typeof(string)] = v => v is "Test",
        // This has a default value of zero, and the test state indicates that this is zero.
        [typeof(int)] = v => v is 0,
        // This is nullable.
        [typeof(CustomFormField)] = v => v is null or CustomFormField
    };

    private Dictionary<Type, object> TypesAfterProcessing = new()
    {
        [typeof(string)] = "Hello, world!",
        [typeof(int)] = 1,
        [typeof(CustomFormField)] = new CustomFormField()
    };

    private sealed class TestFormState : FormState
    {
        // Field with a default value.
        [FormField] public string TestFieldString = "Test";

        // Field with a default, unspecified value.
        [FormField] public int TestFieldInt;

        // Field that's nullable and has a null value by default.
        [FormField] public CustomFormField TestFieldCustomNullDefault = null;
    }

    private sealed class CustomFormField
    {
    }

    // TODO: Custom form constructors with default fallback form UI constructor
    // that reflects over fields/properties.

    [Test]
    public void GrabFormFields()
    {
        var instance = new TestFormState();
        var stateInstance = (FormState) instance;

        Assert.That(stateInstance, Is.InstanceOf<TestFormState>());

        var instanceType = stateInstance.GetType();
        foreach (var property in instanceType.GetMembers())
        {
            // We only care about fields and properties. Continue.
            if (property.MemberType != MemberTypes.Field && property.MemberType != MemberTypes.Property)
            {
                continue;
            }

            // For this test, we need to ensure that our fields all have this attribute.
            Assert.That(property.HasCustomAttribute<FormField>());

            // Get the field/property data. Grab the test function in our type dictionary.
            // We need to be able to get field/property data in order to
            // fill in our form fields. The test function is there to
            // allow us to get a value from the object type.

            // We know the field/property exists at this point, so we ignore null.
            // We cannot use Convert, because that would potentially violate sandboxing,
            // so it's up to the implementor to actually do the object casting. Oops.
            switch (property.MemberType)
            {
                case MemberTypes.Field:
                    var memberField = instanceType.GetField(property.Name)!;
                    var fieldValue = memberField.GetValue(stateInstance);

                    Assert.That(ValidTypesAtInit.TryGetValue(memberField.FieldType, out var fieldFunc));
                    var fieldTruthy = fieldFunc(fieldValue);

                    Assert.That(fieldTruthy);

                    memberField.SetValue(stateInstance, TypesAfterProcessing[memberField.FieldType]);
                    break;
                case MemberTypes.Property:
                    var memberProperty = instanceType.GetProperty(property.Name)!;
                    var propertyValue = memberProperty.GetValue(stateInstance);

                    Assert.That(ValidTypesAtInit.TryGetValue(memberProperty.PropertyType, out var propertyFunc));
                    var propertyTruthy = propertyFunc(propertyValue);

                    Assert.That(propertyTruthy);

                    memberProperty.SetValue(stateInstance, TypesAfterProcessing[memberProperty.PropertyType]);
                    break;
            }
        }

        Assert.Multiple(() =>
            {
                Assert.That(instance.TestFieldInt, Is.EqualTo(1));
                Assert.That(instance.TestFieldString, Is.EqualTo("Hello, world!"));
                Assert.That(instance.TestFieldCustomNullDefault, Is.Not.EqualTo(null));
            }
        );
    }

    public static int TestFunction(object value)
    {
        return TestFunctionInt((int) value);
    }

    public static int TestFunctionInt(int value)
    {
        return value;
    }

    private delegate int IntFormFieldDelegate(object value);

    [Test]
    public void GenerateFormFieldDelegate()
    {
        object test = 2;
        var method = new DynamicMethod("", typeof(int), new[] { typeof(object) });
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Unbox_Any);

        var testFunc = typeof(FormStateTest).GetMethod("TestFunctionInt")!;

        il.Emit(OpCodes.Call, testFunc);
        il.Emit(OpCodes.Ret);

        var fieldDelegate = method.CreateDelegate<IntFormFieldDelegate>();
        Assert.That(fieldDelegate(test), Is.EqualTo(2));
    }
}
*/
