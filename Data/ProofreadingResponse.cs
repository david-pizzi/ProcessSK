using System.ComponentModel;

namespace ProcessSK.Data;

[Serializable]
public class ProofreadingResponse
{
    [Description("An explanation of why the documentation does not meet expectations.")]
    public string Explanation { get; set; } = "";

    [Description("A list of suggestions for improving the documentation.")]
    public List<string> Suggestions { get; set; } = new();
}
