using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace UI.Modules.AccessControl.Services.Attributes;

public interface IAttributeSchemaManagementService
{
    Task<IEnumerable<AttributeSchema>> GetSchemasAsync(string workstream, string? search = null, string? attributeLevel = null);
    Task<IEnumerable<AttributeSchema>> GetActiveSchemasForLevelAsync(string workstream, string attributeLevel);
    Task<AttributeSchema?> GetSchemaByIdAsync(int id);
    Task<(bool Success, AttributeSchema? Schema, string? ErrorMessage)> CreateSchemaAsync(AttributeSchema schema, string workstream, string createdBy);
    Task<(bool Success, string? ErrorMessage)> UpdateSchemaAsync(int id, AttributeSchema schema, string modifiedBy);
    Task<bool> DeleteSchemaAsync(int id);
}
