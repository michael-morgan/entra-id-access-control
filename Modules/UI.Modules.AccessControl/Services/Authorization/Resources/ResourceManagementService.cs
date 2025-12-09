using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;
using Microsoft.Extensions.Logging;

namespace UI.Modules.AccessControl.Services.Authorization.Resources;

/// <summary>
/// Service for managing Casbin resources with business logic.
/// Orchestrates repository calls and ViewModel mapping.
/// </summary>
public class ResourceManagementService(
    IResourceRepository resourceRepository,
    ILogger<ResourceManagementService> logger) : IResourceManagementService
{
    private readonly IResourceRepository _resourceRepository = resourceRepository;
    private readonly ILogger<ResourceManagementService> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinResource>> GetResourcesAsync(
        string selectedWorkstream,
        string? workstreamFilter = null,
        string? search = null)
    {
        return await _resourceRepository.SearchAsync(selectedWorkstream, workstreamFilter, search);
    }

    /// <inheritdoc />
    public async Task<CasbinResource?> GetResourceByIdAsync(int id)
    {
        return await _resourceRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<(bool Success, CasbinResource? Resource, string? ErrorMessage)> CreateResourceAsync(
        ResourceViewModel model, string createdBy)
    {
        var resource = new CasbinResource
        {
            ResourcePattern = model.ResourcePattern,
            WorkstreamId = model.WorkstreamId,
            DisplayName = model.DisplayName,
            Description = model.Description,
            ParentResource = model.ParentResource,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };

        var created = await _resourceRepository.CreateAsync(resource);
        return (true, created, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateResourceAsync(
        int id, ResourceViewModel model, string modifiedBy)
    {
        var resource = await _resourceRepository.GetByIdAsync(id);
        if (resource == null)
        {
            return (false, "Resource not found");
        }

        resource.ResourcePattern = model.ResourcePattern;
        resource.WorkstreamId = model.WorkstreamId;
        resource.DisplayName = model.DisplayName;
        resource.Description = model.Description;
        resource.ParentResource = model.ParentResource;
        resource.ModifiedAt = DateTimeOffset.UtcNow;
        resource.ModifiedBy = modifiedBy;

        await _resourceRepository.UpdateAsync(resource);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteResourceAsync(int id)
    {
        var resource = await _resourceRepository.GetByIdAsync(id);
        if (resource == null)
        {
            return false;
        }

        await _resourceRepository.DeleteAsync(id);
        return true;
    }
}
