namespace NextGenCoaching.Web.Services;

public sealed record ToolBrand(string Name, string File, bool Wide = false, bool DarkBackdrop = false);

// Brand artwork identifies the tools being taught. Generic concepts such as SQL,
// RAG and data modelling deliberately keep a neutral icon instead of a made-up logo.
public static class ToolBrandCatalog
{
    private static readonly Dictionary<string, ToolBrand> Brands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["python"] = new("Python", "python.webp"),
        ["postgresql"] = new("PostgreSQL", "postgresql.svg"),
        ["mysql"] = new("MySQL", "mysql.svg"),
        ["powerbi"] = new("Power BI", "power-bi.svg"),
        ["powerquery"] = new("Power Query", "power-query.svg"),
        ["snowflake"] = new("Snowflake", "snowflake.svg"),
        ["azure"] = new("Microsoft Azure", "azure.svg"),
        ["azuredatafactory"] = new("Azure Data Factory", "azure-data-factory.svg"),
        ["azuresql"] = new("Azure SQL", "azure-sql.svg"),
        ["azureblobstorage"] = new("Azure Blob Storage", "azure-storage.svg"),
        ["pandas"] = new("pandas", "pandas.svg"),
        ["numpy"] = new("NumPy", "numpy.svg"),
        ["matplotlib"] = new("Matplotlib", "matplotlib.svg"),
        ["scikitlearn"] = new("scikit-learn", "scikitlearn.svg"),
        ["tensorflow"] = new("TensorFlow", "tensorflow.svg"),
        ["pytorch"] = new("PyTorch", "pytorch.svg"),
        ["huggingface"] = new("Hugging Face", "huggingface.svg"),
        ["langchain"] = new("LangChain", "langchain.svg", DarkBackdrop: true),
        ["langgraph"] = new("LangGraph", "langgraph.svg", DarkBackdrop: true),
        ["crewai"] = new("CrewAI", "crewai.svg"),
        ["mcp"] = new("Model Context Protocol", "modelcontextprotocol.svg"),
        ["mlflow"] = new("MLflow", "mlflow.svg"),
        ["fastapi"] = new("FastAPI", "fastapi.svg"),
        ["docker"] = new("Docker", "docker.svg"),
        ["pyspark"] = new("Apache Spark", "apachespark.svg"),
        ["databricks"] = new("Databricks", "databricks.svg"),
        ["deltalake"] = new("Delta Lake", "delta-lake.svg", Wide: true, DarkBackdrop: true),
        ["unitycatalog"] = new("Unity Catalog", "unity-catalog.png", Wide: true)
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["powerbidesktop"] = "powerbi",
        ["adf"] = "azuredatafactory",
        ["microsoftazure"] = "azure",
        ["azuresqldatabase"] = "azuresql",
        ["snowpipe"] = "snowflake",
        ["snowpark"] = "snowflake",
        ["apachespark"] = "pyspark",
        ["modelcontextprotocol"] = "mcp",
        ["autoloader"] = "databricks",
        ["declarativepipelines"] = "databricks"
    };

    public static ToolBrand? Find(string name)
    {
        var key = string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (Aliases.TryGetValue(key, out var alias)) key = alias;
        return Brands.GetValueOrDefault(key);
    }

    public static ToolBrand[] FindAll(string name) => name
        .Split('&', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(Find).OfType<ToolBrand>().Distinct().ToArray();

    public static string ForCourse(string slug) => slug switch
    {
        "data-science" or "python" => "Python",
        "sql" => "PostgreSQL",
        "power-bi" => "Power BI",
        "snowflake" => "Snowflake",
        "azure-data-factory" => "Azure Data Factory",
        "advanced-ai" => "PyTorch",
        "machine-learning" => "scikit-learn",
        "generative-ai" => "Hugging Face",
        "agentic-ai" => "LangGraph",
        "databricks" => "Databricks",
        _ => string.Empty
    };

    public static string NeutralIcon(string name) =>
        name.Contains("SQL", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("database", StringComparison.OrdinalIgnoreCase) ? "database" : "code";
}
