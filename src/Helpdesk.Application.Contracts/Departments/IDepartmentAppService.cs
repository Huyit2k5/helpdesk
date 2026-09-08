using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Departments;

public interface IDepartmentAppService : ICrudAppService<
    DepartmentDto,
    Guid,
    DepartmentGetListInput,
    CreateUpdateDepartmentDto>
{
}
