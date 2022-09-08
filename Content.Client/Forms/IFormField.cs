namespace Content.Client.Forms;

public interface IFormField
{
    /// <summary>
    ///     Callback that occurs whenever the value of this field is set by the user.
    /// </summary>
    public Action? OnFieldSet { get; }

    public string FieldName { get; set; }

    /// <summary>
    ///     Value in this form field.
    /// </summary>
    public object Value { get; set; }
}
