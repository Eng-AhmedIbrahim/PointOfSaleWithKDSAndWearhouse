using POS.Contract.Dtos.CompanyDtos;

namespace BlazorBase.ERPFrontServices.CompanyServices;

public interface ICompanyService
{
    Task<IReadOnlyList<CreateCompanyDto>> GetCompanies();
    Task<CreateCompanyDto?> GetCompanyById(int id);
    Task<CreateCompanyDto?> CreateCompany(CreateCompanyDto companyDto);
    Task<CreateCompanyDto?> UpdateCompany(UpdatedCompanyDto companyDto);
    Task<bool> DeleteCompany(int id);
}
