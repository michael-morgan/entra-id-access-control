using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;

namespace UI.Modules.AccessControl.Services.Attributes;

public class AttributeSchemaManagementService(IAttributeSchemaRepository schemaRepository) : IAttributeSchemaManagementService
{
    private readonly IAttributeSchemaRepository _schemaRepository = schemaRepository;

    public async Task<IEnumerable<AttributeSchema>> GetSchemasAsync(string workstream, string? search = null, string? attributeLevel = null)
    {
        return await _schemaRepository.SearchAsync(workstream, search, attributeLevel);
    }

    public async Task<IEnumerable<AttributeSchema>> GetActiveSchemasForLevelAsync(string workstream, string attributeLevel)
    {
        return await _schemaRepository.GetActiveByLevelAsync(workstream, attributeLevel);
    }

    public async Task<AttributeSchema?> GetSchemaByIdAsync(int id)
    {
        return await _schemaRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, AttributeSchema? Schema, string? ErrorMessage)> CreateSchemaAsync(AttributeSchema schema, string workstream, string createdBy)
    {
        schema.WorkstreamId = workstream;
        schema.CreatedBy = createdBy;
        var created = await _schemaRepository.CreateAsync(schema);
        return (true, created, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateSchemaAsync(int id, AttributeSchema schema, string modifiedBy)
    {
        var existing = await _schemaRepository.GetByIdAsync(id);
        if (existing == null) return (false, "Schema not found");

        schema.ModifiedBy = modifiedBy;
        await _schemaRepository.UpdateAsync(schema);
        return (true, null);
    }

    public async Task<bool> DeleteSchemaAsync(int id)
    {
        var schema = await _schemaRepository.GetByIdAsync(id);
        if (schema == null) return false;

        await _schemaRepository.DeleteAsync(id);
        return true;
    }
}
