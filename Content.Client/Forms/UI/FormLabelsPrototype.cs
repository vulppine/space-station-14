using Robust.Shared.Prototypes;

namespace Content.Client.Forms.UI;

[Prototype("formLabels")]
public sealed class FormLabelsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField("labels")] public readonly Dictionary<string, string> Labels = new();
}
