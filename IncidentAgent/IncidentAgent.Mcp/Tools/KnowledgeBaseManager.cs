using IncidentAgent.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IncidentAgent.Mcp.Tools
{
    [McpServerToolType]
    [Description("MCP Server Tools to read and write KABLAM Company Knowledge base of common incidents, including MELTCORE 6 Incidents")]
    public class KnowledgeBaseManager
    {
        private readonly KnowledgeBaseRepository _knowledgeBaseRepository;
        public KnowledgeBaseManager(KnowledgeBaseRepository knowledgeBaseRepository)
        {
            _knowledgeBaseRepository = knowledgeBaseRepository;
        }

        [McpServerTool]
        [Description("Search KABLAM Company incident resolution knowledge base")]
        public Task<List<KnowledgeBaseEntry>> SearchKnowledgeBase([Description("Search query")] string query)
        {
            return _knowledgeBaseRepository
                .SearchKnowledgeBaseEntries(query);
        }

        [McpServerTool]
        [Description("Add a new knowledge base entry to our KABLAM Company incident resolution knowledge base")]
        public Task AddKnowledgeEntry([Description("New Incident resolution Knowledge Base Entry to be added")] KnowledgeBaseEntry entry)
        {
            return _knowledgeBaseRepository
                .AddKnowledgeBaseEntry(entry);
        }
    }
}
