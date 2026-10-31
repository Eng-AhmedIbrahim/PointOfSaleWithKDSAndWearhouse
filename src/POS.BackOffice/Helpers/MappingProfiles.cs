using POS.Contract.Dtos.CategoryDtos;

namespace POS.BackOffice.Helpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CategoryToReturnDto, UpdatedCategoryDto>();
    }
}
