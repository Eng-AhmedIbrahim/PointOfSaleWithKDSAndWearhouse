namespace POS.Core.Services.Contract.CompanyService;

public interface ICompanyService
{
    public Task<Company?> CrateCompanyAsync(Company company);
    public Task<Company?> UpdateCompanyAsync(Company oldCompany, Company newCompanys);
    public Task<IReadOnlyList<Company>?> GetCompaniesAsync();
    public Task<Company?>? GetCompanyByIdAsync(int companyId);
    public Task<bool> DeleteCompany(Company company);
}