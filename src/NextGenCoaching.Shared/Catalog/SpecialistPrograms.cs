using NextGenCoaching.Shared.Dtos;

namespace NextGenCoaching.Shared.Catalog;

/// <summary>Additional AI and lakehouse programs shared by initial seeding and offline previews.</summary>
public static class SpecialistPrograms
{
    public static IReadOnlyList<CourseDetailDto> All { get; } = new[]
    {
        Build("77777777-7777-7777-7777-777777777777", "advanced-ai", "Advanced Artificial Intelligence",
            "Build a complete AI foundation, from Python and machine learning to generative models, agents and deployment.",
            "A structured path for working professionals who want to build applied AI systems. Start with Python, SQL and statistics, progress through machine learning and deep learning, then create retrieval-augmented applications and agent workflows. Practise evaluation, API delivery and deployment through guided projects.",
            6, "ai", 7,
            [("Python & SQL", "Data preparation and analytical foundations"), ("scikit-learn", "Predictive models and evaluation"), ("PyTorch", "Neural networks and deep learning"), ("Hugging Face", "Model and embedding workflows"), ("MLflow", "Experiment tracking and model lifecycle"), ("FastAPI & Docker", "Serving and packaging AI applications")],
            ["Python, SQL, statistics, machine learning and deep learning", "Generative AI, retrieval-augmented generation and agent workflows", "Applied projects across business use cases", "Model evaluation, MLOps and cloud deployment"],
            [("Programming and data foundations", "Prepare data with Python and SQL; review statistics and probability."), ("Machine learning", "Train, validate and explain supervised and unsupervised models."), ("Deep learning", "Build neural networks and explore language and vision tasks."), ("Generative AI and retrieval", "Use embeddings and retrieval to ground model responses in documents."), ("Agent workflows", "Connect models to tools with controlled, observable workflows."), ("Deploy an AI application", "Package an evaluated application with an API, containers and monitoring.")]),
        Build("88888888-8888-8888-8888-888888888888", "machine-learning", "Machine Learning",
            "Turn data into predictions with Python, statistics, machine learning and practical model evaluation.",
            "Develop the skills to prepare datasets, train predictive models and explain their results. Work with Python, SQL, scikit-learn and neural network tools while exploring predictive analytics, natural language processing, computer vision and recommendation systems. Projects connect modelling decisions to realistic business problems.",
            3, "ai", 8,
            [("Python & SQL", "Prepare and explore data"), ("scikit-learn", "Classification, regression and clustering"), ("TensorFlow", "Deep learning foundations"), ("PyTorch", "Neural network experimentation"), ("Hugging Face", "Language and vision models")],
            ["Python, SQL, statistics and model-building foundations", "Predictive analytics and recommendation systems", "NLP, computer vision and deep learning practice", "Projects using banking, healthcare, retail and insurance datasets"],
            [("Data and statistics", "Explore distributions, missing values, feature quality and leakage."), ("Supervised learning", "Compare regression and classification models with sound validation."), ("Unsupervised learning", "Explore clusters, dimensionality reduction and recommendations."), ("Deep learning", "Build and evaluate neural networks with modern frameworks."), ("Language and vision", "Apply model workflows to text and image datasets."), ("Applied ML project", "Present a reproducible model with metrics, explanations and limitations.")]),
        Build("99999999-9999-9999-9999-999999999999", "generative-ai", "Generative AI",
            "Create grounded AI applications with LLMs, prompt engineering, embeddings, RAG and vector search.",
            "Learn to build useful applications with language models rather than relying on prompts alone. Develop document search and retrieval-augmented generation, connect tools through model workflows and MCP, and evaluate response quality. Build a copilot or enterprise-search application and serve it with FastAPI and Docker.",
            3, "ai", 9,
            [("LLM APIs", "Integrate hosted language models"), ("LangChain", "Retrieval and application workflows"), ("Hugging Face", "Models, tokenizers and embeddings"), ("Vector databases", "Semantic indexing and similarity search"), ("FastAPI & Docker", "Deliver an application API")],
            ["LLMs, prompt engineering, embeddings and retrieval-augmented generation", "AI copilots, enterprise search and grounded question answering", "Tool integration, agent fundamentals and MCP", "Evaluation, responsible use and application deployment"],
            [("Language model foundations", "Understand tokens, context, prompting and model limitations."), ("Embeddings and vector search", "Create a semantic index and measure retrieval quality."), ("Build a RAG pipeline", "Connect document ingestion, retrieval and grounded responses."), ("Copilots and tools", "Add structured outputs, tool calls and MCP integrations."), ("Evaluate your application", "Check relevance, faithfulness, cost, latency and failure handling."), ("Deploy a GenAI project", "Serve a documented application through FastAPI and containers.")]),
        Build("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "agentic-ai", "Agentic AI",
            "Build AI agents that plan, use tools and coordinate reliable multi-step workflows.",
            "Design tool-using AI systems for working professionals with programming experience. Start with LLM and retrieval foundations, then build stateful workflows with LangGraph and coordinated agents with CrewAI. Explore MCP integrations, human approval steps, tracing and evaluation before deploying a practical automation project.",
            5, "ai", 10,
            [("LangGraph", "Stateful agent workflows"), ("CrewAI", "Role-based multi-agent coordination"), ("LangChain", "Model and retrieval integrations"), ("MCP", "Standardised tool and context connections"), ("Vector databases", "Retrieval for grounded agent responses"), ("FastAPI & Docker", "API delivery and deployment")],
            ["LLMs, prompt engineering, RAG, agents and MCP", "Tool-using copilots and multi-agent automation", "State, memory, human review and workflow observability", "Evaluation and production application deployment"],
            [("LLM and retrieval foundations", "Ground agent behaviour in prompts, schemas and retrieved context."), ("Tools and MCP", "Connect external tools with explicit inputs and access boundaries."), ("Stateful workflows", "Build predictable graph-based flows with LangGraph."), ("Multi-agent coordination", "Explore role-based collaboration with CrewAI."), ("Reliability and evaluation", "Add tracing, human checkpoints, retries and task-level evaluation."), ("Deploy an agent system", "Deliver a containerised workflow with documented operating limits.")]),
        Build("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "databricks", "Databricks Data Engineering",
            "Build lakehouse pipelines with PySpark, Delta Lake, Unity Catalog and medallion architecture.",
            "Develop practical data engineering skills in Databricks. Explore the workspace and lakehouse architecture, use PySpark and SQL to transform data, and build incremental pipelines with Auto Loader and declarative pipelines. Practise change data capture, Unity Catalog governance, data quality and performance optimisation across bronze, silver and gold layers.",
            4, "cloud", 11,
            [("Databricks", "Workspace, jobs and compute"), ("PySpark & SQL", "Distributed data transformations"), ("Delta Lake", "Reliable lakehouse tables"), ("Unity Catalog", "Governance and access management"), ("Auto Loader", "Incremental ingestion"), ("Declarative pipelines", "Lakeflow Spark Declarative Pipelines, formerly DLT")],
            ["Databricks workspace, Delta Lake, Unity Catalog and lakehouse foundations", "Enterprise ETL with PySpark, Auto Loader and declarative pipelines", "Change data capture and bronze, silver and gold layers", "Data quality, governance and performance optimisation"],
            [("Workspace and lakehouse", "Explore compute, notebooks, jobs and lakehouse table design."), ("PySpark transformations", "Clean and join datasets with distributed DataFrames and SQL."), ("Delta Lake", "Work with transactional tables, schema evolution and incremental changes."), ("Ingestion and CDC", "Build Auto Loader ingestion and change-data-capture workflows."), ("Governed medallion pipelines", "Organise bronze, silver and gold data with quality checks and Unity Catalog."), ("Operate a data platform", "Schedule, monitor and tune an end-to-end lakehouse project.")])
    };

    public static string? DurationLabel(string slug) => slug switch
    {
        "advanced-ai" => "5–6 months", "machine-learning" or "generative-ai" => "2–3 months",
        "agentic-ai" => "4–5 months", "databricks" => "3–4 months", _ => null
    };

    private static CourseDetailDto Build(string id, string slug, string title, string tagline, string description,
        int months, string category, int order, (string Name, string Description)[] tools, string[] highlights,
        (string Title, string Description)[] steps) => new()
    {
        Id = Guid.Parse(id), Slug = slug, Title = title, Tagline = tagline, Description = description,
        DurationMonths = months, Mode = "live-online", Level = "intermediate", Category = category,
        Currency = "INR", SortOrder = order,
        HeroImageUrl = $"/images/courses/{slug}.svg",
        SeoTitle = $"{title} | Live Online Training | NextGen",
        SeoDescription = tagline,
        Tools = tools.Select((t, i) => new CourseToolDto { ToolName = t.Name, Description = t.Description, SortOrder = i + 1 }).ToArray(),
        Features = highlights.Select((h, i) => new CourseFeatureDto { Label = h, SortOrder = i + 1 }).ToArray(),
        JourneySteps = steps.Select((s, i) => new CourseJourneyStepDto { StepNumber = i + 1, Title = s.Title, Description = s.Description }).ToArray()
    };
}
