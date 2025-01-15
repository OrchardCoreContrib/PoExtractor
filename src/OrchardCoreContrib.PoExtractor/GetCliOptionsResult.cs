namespace OrchardCoreContrib.PoExtractor;

public class GetCliOptionsResult
{
    public string Language { get; set; }
    public string TemplateEngine { get; set; }
    public string SingleOutputFile { get; set; }
    public IList<string> Plugins { get; set; } = new List<string>();
}
