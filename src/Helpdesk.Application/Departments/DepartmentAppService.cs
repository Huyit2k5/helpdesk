using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Departments;

[Authorize(HelpdeskPermissions.Departments.Default)]
public class DepartmentAppService : CrudAppService<
    Department,
    DepartmentDto,
    Guid,
    DepartmentGetListInput,
    CreateUpdateDepartmentDto>,
    IDepartmentAppService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly DepartmentManager _departmentManager;

    public DepartmentAppService(
        IDepartmentRepository departmentRepository,
        DepartmentManager departmentManager)
        : base(departmentRepository)
    {
        _departmentRepository = departmentRepository;
        _departmentManager = departmentManager;

        GetPolicyName = HelpdeskPermissions.Departments.Default;
        GetListPolicyName = HelpdeskPermissions.Departments.Default;
        CreatePolicyName = HelpdeskPermissions.Departments.Create;
        UpdatePolicyName = HelpdeskPermissions.Departments.Edit;
        DeletePolicyName = HelpdeskPermissions.Departments.Delete;
    }

    [Authorize(HelpdeskPermissions.Departments.Create)]
    public override async Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input)
    {
        var entity = await _departmentManager.CreateAsync(
            input.Name, input.Code, input.ManagerId,
            input.Description, input.IsActive);

        await _departmentRepository.InsertAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    [Authorize(HelpdeskPermissions.Departments.Edit)]
    public override async Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input)
    {
        var entity = await _departmentRepository.GetAsync(id);

        entity.SetName(input.Name);
        await _departmentManager.ChangeCodeAsync(entity, input.Code);
        entity.ManagerId = input.ManagerId;
        entity.Description = input.Description;
        entity.IsActive = input.IsActive;

        await _departmentRepository.UpdateAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    protected override async Task<IQueryable<Department>> CreateFilteredQueryAsync(DepartmentGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        return queryable
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Filter!) || x.Code.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue,
                x => x.IsActive == input.IsActive!.Value);
    }

    protected override Task<DepartmentDto> MapToGetOutputDtoAsync(Department entity) =>
        Task.FromResult(MapToDto(entity));

    protected override Task<DepartmentDto> MapToGetListOutputDtoAsync(Department entity) =>
        Task.FromResult(MapToDto(entity));

    private static DepartmentDto MapToDto(Department entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        ManagerId = entity.ManagerId,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId
    };
}
