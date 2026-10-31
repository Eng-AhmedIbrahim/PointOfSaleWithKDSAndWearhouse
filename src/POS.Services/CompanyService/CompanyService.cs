namespace POS.Services.CompanyService;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Company?> CrateCompanyAsync(Company company)
    {
        try
        {
            if (company is null)
                return null;

            await _unitOfWork.Repository<Company>().AddAsync(company);
            
            var result = await _unitOfWork.CompleteAsync();   
            if(result <= 0)
                return null;

            return company;
        }
        catch(Exception ex) 
        {
            Log.Error(ex, "An error occurred while creating the company.");
            return null;
        }
    }

    public async Task<IReadOnlyList<Company>?> GetCompaniesAsync()
    {
        try
        {
            var companies = await _unitOfWork.Repository<Company>().GetAllAsync();

            if (companies is null)
                return null;

            return companies;
        }catch(Exception ex) 
        {
            Log.Error(ex, "There Are Not Companies");
            return null;
        }
    }

    public async Task<Company?> GetCompanyByIdAsync(int companyId)
    {
        try 
        {
            var company = await _unitOfWork.Repository<Company>().GetByIdAsync(companyId);

            if (company is null)
                return null;

            return company;
        }
        catch (Exception ex) 
        {
            Log.Error(ex, "There Are Not Company With This Id {companyId}", companyId);
            return null;
        }

    }

    public async Task<Company?> UpdateCompanyAsync(Company oldCompany,Company newCompany)
    {
        try
        {

            oldCompany.Id = oldCompany.Id;
            if(!string.IsNullOrEmpty(newCompany.ArabicName))
                oldCompany.ArabicName = newCompany.ArabicName;
            if (!string.IsNullOrEmpty(newCompany.EnglishName))
            {
                oldCompany.EnglishName = newCompany.EnglishName;
                oldCompany.NormalizedEnglishName = newCompany.EnglishName.ToUpper();
            }
            if (!string.IsNullOrEmpty(newCompany.Email))
            {
                oldCompany.Email = newCompany.Email;
                oldCompany.NormalizedEmail = newCompany.Email.ToUpper();
            }
            if (!string.IsNullOrEmpty(newCompany.Address))
                oldCompany.Address =  newCompany.Address;
            if (!string.IsNullOrEmpty(newCompany.MobileNumber))
                oldCompany.MobileNumber = newCompany.MobileNumber;
            if (!string.IsNullOrEmpty(newCompany.PhoneNumber))
                oldCompany.PhoneNumber = newCompany.PhoneNumber;
            if (!string.IsNullOrEmpty(newCompany.Website))
                oldCompany.Website = newCompany.Website;

            _unitOfWork.Repository<Company>().Update(oldCompany);

            var result =  await _unitOfWork.CompleteAsync();
            if(result <= 0)
                return null;

            return oldCompany ?? null;
        }
        catch(Exception ex)
        {
            Log.Error(ex, "Error Occur During Update Company That Have Id {companyId}", oldCompany.Id);
            return null;
        }
    }

    public async Task<bool> DeleteCompany(Company company)
    {
        try
        {
            if (company is null)
                return false;

            _unitOfWork.Repository<Company>().Delete(company);

            var result  = await _unitOfWork.CompleteAsync();
            if(result <= 0) 
                return false;
            
            return true;

        }catch(Exception ex)
        {
            Log.Error(ex,"Cant Delete Company With Id {companyId}",company.Id);
            return false;   
        }
    }
    
}