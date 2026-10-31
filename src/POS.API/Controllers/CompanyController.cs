using POS.Contract.Dtos.CompanyDtos;

namespace POS.API.Controllers;

public class CompanyController : BaseApiController
{
    private readonly ICompanyService _companyService;
    private readonly IMapper _mapper;

    public CompanyController(ICompanyService companyService, IMapper mapper)
    {
        _companyService = companyService;
        _mapper = mapper;
    }


    [ProducesResponseType(typeof(Company), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto companyDto)
    {
        if (companyDto is null)
            return BadRequest(new ApiResponse(400));

        var mappedCompany = _mapper.Map<CreateCompanyDto, Company>(companyDto);

        var company = await _companyService.CrateCompanyAsync(mappedCompany);

        if (company is null)
            return BadRequest(new ApiResponse(400));

        return Ok(company);
    }

    [ProducesResponseType(typeof(IReadOnlyList<Company>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetAllCompanies")]
    public async Task<IActionResult> GetAllCompanies()
    {
        var companies = await _companyService.GetCompaniesAsync();
        if (companies is null)
            return NotFound(new ApiResponse(404));

        return Ok(companies);
    }


    [ProducesResponseType(typeof(Company), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{companyId}")]
    public async Task<IActionResult> GetCompanyById(int companyId)
    {
        Company? company = await _companyService!.GetCompanyByIdAsync(companyId)!;
        if (company is null)
            return NotFound(new ApiResponse(404));

        return Ok(company);
    }


    [ProducesResponseType(typeof(Company), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdatedCompanyDto company)
    {
        Company? storedCompany = await _companyService!.GetCompanyByIdAsync(company!.CompanyId!)!;
        var newCompany = _mapper.Map<UpdatedCompanyDto,Company>(company);
        newCompany.Id = storedCompany!.Id;   

        if (storedCompany is null)
            return NotFound(new ApiResponse(404));

        var result = await _companyService.UpdateCompanyAsync(storedCompany, newCompany);

        if (result is null)
            return NotFound(new ApiResponse(404));

        return Ok(result);
    }


    [ProducesResponseType(typeof(Company), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete]
    public async Task<IActionResult> DeleteCompany(int companyId)
    {
        var company = await _companyService!.GetCompanyByIdAsync(companyId)!;
        if (company is null)
            return NotFound(new ApiResponse(404));

       var result = await _companyService.DeleteCompany(company);
        if(result is false)
            return NotFound(new ApiResponse(404));

        return Ok("CompanyDeletedSuccessfully");
    }
}