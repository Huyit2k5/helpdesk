using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Categories;

[Authorize(HelpdeskPermissions.Categories.Default)]
public class CategoryAppService : CrudAppService<
    Category,
    CategoryDto,
    Guid,
    CategoryGetListInput,
    CreateUpdateCategoryDto>,
    ICategoryAppService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryManager _categoryManager;

    public CategoryAppService(
        ICategoryRepository categoryRepository,
        CategoryManager categoryManager)
        : base(categoryRepository)
    {
        _categoryRepository = categoryRepository;
        _categoryManager = categoryManager;

        GetPolicyName = HelpdeskPermissions.Categories.Default;
        GetListPolicyName = HelpdeskPermissions.Categories.Default;
        CreatePolicyName = HelpdeskPermissions.Categories.Create;
        UpdatePolicyName = HelpdeskPermissions.Categories.Edit;
        DeletePolicyName = HelpdeskPermissions.Categories.Delete;
    }

    [Authorize(HelpdeskPermissions.Categories.Create)]
    public override async Task<CategoryDto> CreateAsync(CreateUpdateCategoryDto input)
    {
        var entity = await _categoryManager.CreateAsync(
            input.Name,
            input.Code,
            input.ParentId,
            input.Description,
            input.IsActive,
            input.Order);

        await _categoryRepository.InsertAsync(entity, autoSave: true);

        return await MapToGetOutputDtoAsync(entity);
    }

    [Authorize(HelpdeskPermissions.Categories.Edit)]
    public override async Task<CategoryDto> UpdateAsync(Guid id, CreateUpdateCategoryDto input)
    {
        var entity = await _categoryRepository.GetAsync(id);

        entity.SetName(input.Name);
        await _categoryManager.ChangeCodeAsync(entity, input.Code);
        entity.ParentId = input.ParentId;
        entity.Description = input.Description;
        entity.IsActive = input.IsActive;
        entity.Order = input.Order;

        await _categoryRepository.UpdateAsync(entity, autoSave: true);

        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<ListResultDto<CategoryLookupDto>> GetLookupAsync()
    {
        var categories = await _categoryRepository.GetListAsync(isActive: true);

        return new ListResultDto<CategoryLookupDto>(
            categories.Select(x => new CategoryLookupDto
            {
                Id = x.Id,
                Name = x.Name
            }).ToList());
    }

    protected override async Task<IQueryable<Category>> CreateFilteredQueryAsync(CategoryGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        return queryable
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Filter!) || x.Code.Contains(input.Filter!))
            .WhereIf(input.ParentId.HasValue,
                x => x.ParentId == input.ParentId)
            .WhereIf(input.IsActive.HasValue,
                x => x.IsActive == input.IsActive!.Value);
    }

    protected override Task<CategoryDto> MapToGetOutputDtoAsync(Category entity)
    {
        var dto = new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            ParentId = entity.ParentId,
            Description = entity.Description,
            IsActive = entity.IsActive,
            Order = entity.Order,
            CreationTime = entity.CreationTime,
            CreatorId = entity.CreatorId,
            LastModificationTime = entity.LastModificationTime,
            LastModifierId = entity.LastModifierId
        };

        return Task.FromResult(dto);
    }

    protected override Task<CategoryDto> MapToGetListOutputDtoAsync(Category entity)
    {
        return MapToGetOutputDtoAsync(entity);
    }
}
