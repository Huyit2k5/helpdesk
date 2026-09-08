using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Helpdesk.Categories;

public class CategoryManager : DomainService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryManager(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Category> CreateAsync(
        string name,
        string code,
        Guid? parentId = null,
        string? description = null,
        bool isActive = true,
        int order = 0)
    {
        await ValidateCodeAsync(code);

        return new Category(
            GuidGenerator.Create(),
            name,
            code,
            parentId,
            description,
            isActive,
            order);
    }

    public async Task ChangeCodeAsync(Category category, string newCode)
    {
        Check.NotNull(category, nameof(category));

        if (category.Code == newCode)
        {
            return;
        }

        await ValidateCodeAsync(newCode);
        category.SetCode(newCode);
    }

    private async Task ValidateCodeAsync(string code)
    {
        var existing = await _categoryRepository.FindByCodeAsync(code);
        if (existing != null)
        {
            throw new BusinessException(HelpdeskDomainErrorCodes.CategoryCodeAlreadyExists)
                .WithData("code", code);
        }
    }
}
