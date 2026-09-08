using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Categories;

public interface ICategoryAppService : ICrudAppService<
    CategoryDto,
    Guid,
    CategoryGetListInput,
    CreateUpdateCategoryDto>
{
    Task<ListResultDto<CategoryLookupDto>> GetLookupAsync();
}
