using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Helpdesk.Departments;

public class DepartmentManager : DomainService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentManager(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Department> CreateAsync(
        string name,
        string code,
        Guid? managerId = null,
        string? description = null,
        bool isActive = true)
    {
        await ValidateCodeAsync(code);

        return new Department(
            GuidGenerator.Create(),
            name,
            code,
            managerId,
            description,
            isActive);
    }

    public async Task ChangeCodeAsync(Department department, string newCode)
    {
        Check.NotNull(department, nameof(department));

        if (department.Code == newCode)
        {
            return;
        }

        await ValidateCodeAsync(newCode);
        department.SetCode(newCode);
    }

    private async Task ValidateCodeAsync(string code)
    {
        var existing = await _departmentRepository.FindByCodeAsync(code);
        if (existing != null)
        {
            throw new BusinessException(HelpdeskDomainErrorCodes.DepartmentCodeAlreadyExists)
                .WithData("code", code);
        }
    }
}
