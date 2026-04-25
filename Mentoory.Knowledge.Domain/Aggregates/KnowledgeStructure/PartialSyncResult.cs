namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;

/// <summary>
/// Summary of the mutations applied by <see cref="KnowledgeStructure.ApplyPartialSync(Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate.KnowledgeStructureTemplate)"/>.
/// </summary>
public sealed class PartialSyncResult
{
    public int ModulesAdded { get; internal set; }

    public int TopicsAdded { get; internal set; }

    public int SubjectsAdded { get; internal set; }

    public int ResourcesAdded { get; internal set; }
}
