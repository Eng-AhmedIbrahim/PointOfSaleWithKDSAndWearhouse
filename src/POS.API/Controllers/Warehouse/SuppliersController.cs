using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

//[Authorize]
[ApiController]
[Route("api/warehouse/suppliers")]
[Route("api/warehouse/supplier")]
[Route("api/suppliers")]
[Route("api/supplier")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;
    public SuppliersController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        var query = _db.WH_Suppliers.AsQueryable();
        if (activeOnly == true) query = query.Where(s => s.IsActive);
        return Ok(await query.OrderBy(s => s.NameAr).Select(s => new
        {
            s.Id, s.Code, Name = s.NameAr, s.NameAr, s.NameEn, s.Phone, s.Email, Address = s.Address, City = s.Address, ContactPerson = s.NameEn ?? "",
            s.TaxNumber, s.CommercialRegister, s.DefaultPaymentTerm,
            s.CreditLimit, s.CurrentBalance, s.IsActive, s.CreatedAt
        }).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _db.WH_Suppliers.Include(x => x.PriceContracts).ThenInclude(p => p.Product)
            .Include(x => x.Invoices).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        return Ok(new
        {
            s.Id,
            s.Code,
            Name = s.NameAr,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            ContactPerson = s.NameEn ?? "",
            Phone = s.Phone ?? "",
            Email = s.Email ?? "",
            Address = s.Address ?? "",
            City = s.Address ?? "",
            TaxNumber = s.TaxNumber ?? "",
            CommercialRegister = s.CommercialRegister ?? "",
            BankName = s.BankName ?? "",
            Iban = s.Iban ?? "",
            PaymentTermsDays = (int)s.DefaultPaymentTerm * 15,
            DefaultPaymentTerm = s.DefaultPaymentTerm,
            CreditLimit = s.CreditLimit,
            CurrentBalance = s.CurrentBalance,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            Notes = "",
            PriceContracts = s.PriceContracts.Select(pc => new
            {
                pc.Id,
                pc.ProductId,
                UnitPrice = pc.AgreedPrice,
                StartDate = (DateTime?)pc.ValidFrom,
                EndDate = pc.ValidTo,
                IsPreferred = pc.IsActive
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierInputDto dto)
    {
        var nameAr = !string.IsNullOrWhiteSpace(dto.NameAr) ? dto.NameAr : (!string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : "");
        if (string.IsNullOrWhiteSpace(nameAr))
            return BadRequest(new { message = "اسم المورد مطلوب" });

        var code = dto.Code?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            var nextNum = await _db.WH_Suppliers.CountAsync() + 1;
            code = $"SUP-{nextNum:D4}";
            while (await _db.WH_Suppliers.AnyAsync(s => s.Code == code))
            {
                nextNum++;
                code = $"SUP-{nextNum:D4}";
            }
        }
        else if (await _db.WH_Suppliers.AnyAsync(s => s.Code == code))
        {
            return BadRequest(new { message = "كود المورد موجود مسبقاً" });
        }

        var supplier = new Supplier
        {
            Code = code,
            NameAr = nameAr,
            NameEn = !string.IsNullOrWhiteSpace(dto.ContactPerson) ? dto.ContactPerson : (dto.NameEn ?? ""),
            Phone = dto.Phone,
            Email = dto.Email,
            Address = !string.IsNullOrWhiteSpace(dto.Address) ? dto.Address : dto.City,
            TaxNumber = dto.TaxNumber,
            CommercialRegister = dto.CommercialRegister,
            BankName = dto.BankName,
            Iban = dto.Iban,
            DefaultPaymentTerm = dto.DefaultPaymentTerm ?? (dto.PaymentTermsDays.HasValue ? (PaymentTermType)Math.Clamp((dto.PaymentTermsDays.Value / 15) + 1, 1, 6) : PaymentTermType.Cash),
            CreditLimit = dto.CreditLimit ?? 0m,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.PriceContracts != null)
        {
            foreach (var pc in dto.PriceContracts.Where(p => p.ProductId > 0 && p.UnitPrice > 0))
            {
                var prod = await _db.WarehouseProducts.FindAsync(pc.ProductId);
                supplier.PriceContracts.Add(new SupplierPriceContract
                {
                    ProductId = pc.ProductId,
                    UnitId = prod?.BaseUnitId ?? 1,
                    AgreedPrice = pc.UnitPrice,
                    ValidFrom = pc.StartDate ?? DateTime.UtcNow,
                    ValidTo = pc.EndDate,
                    IsActive = true
                });
            }
        }

        _db.WH_Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierInputDto dto)
    {
        var existing = await _db.WH_Suppliers.Include(s => s.PriceContracts).FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return NotFound();

        var nameAr = !string.IsNullOrWhiteSpace(dto.NameAr) ? dto.NameAr : (!string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.NameAr);
        existing.NameAr = nameAr;
        if (!string.IsNullOrWhiteSpace(dto.ContactPerson)) existing.NameEn = dto.ContactPerson;
        else if (dto.NameEn != null) existing.NameEn = dto.NameEn;

        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != existing.Code)
        {
            if (await _db.WH_Suppliers.AnyAsync(s => s.Code == dto.Code && s.Id != id))
                return BadRequest(new { message = "كود المورد مستخدم مسبقاً" });
            existing.Code = dto.Code;
        }

        existing.Phone = dto.Phone;
        existing.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Address)) existing.Address = dto.Address;
        else if (!string.IsNullOrWhiteSpace(dto.City)) existing.Address = dto.City;
        existing.TaxNumber = dto.TaxNumber;
        existing.CommercialRegister = dto.CommercialRegister;
        existing.BankName = dto.BankName;
        existing.Iban = dto.Iban;
        if (dto.DefaultPaymentTerm.HasValue) existing.DefaultPaymentTerm = dto.DefaultPaymentTerm.Value;
        if (dto.CreditLimit.HasValue) existing.CreditLimit = dto.CreditLimit.Value;
        existing.IsActive = dto.IsActive;

        if (dto.PriceContracts != null)
        {
            _db.SupplierPriceContracts.RemoveRange(existing.PriceContracts);
            foreach (var pc in dto.PriceContracts.Where(p => p.ProductId > 0 && p.UnitPrice > 0))
            {
                var prod = await _db.WarehouseProducts.FindAsync(pc.ProductId);
                existing.PriceContracts.Add(new SupplierPriceContract
                {
                    SupplierId = id,
                    ProductId = pc.ProductId,
                    UnitId = prod?.BaseUnitId ?? 1,
                    AgreedPrice = pc.UnitPrice,
                    ValidFrom = pc.StartDate ?? DateTime.UtcNow,
                    ValidTo = pc.EndDate,
                    IsActive = true
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _db.WH_Suppliers.Include(s => s.PriceContracts).FirstOrDefaultAsync(s => s.Id == id);
        if (supplier == null) return NotFound();

        bool hasInvoices = await _db.SupplierInvoices.AnyAsync(i => i.SupplierId == id);
        bool hasPOs = await _db.PurchaseOrders.AnyAsync(p => p.SupplierId == id);
        if (hasInvoices || hasPOs)
        {
            supplier.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "تم إلغاء تنشيط المورد لوجود حركات سابقة مرتبطة به" });
        }

        _db.SupplierPriceContracts.RemoveRange(supplier.PriceContracts);
        _db.WH_Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
        return Ok(new { message = "تم حذف المورد بنجاح" });
    }

    // Price Contracts
    [HttpGet("{supplierId}/prices")]
    public async Task<IActionResult> GetPrices(int supplierId)
    {
        var prices = await _db.SupplierPriceContracts
            .Include(p => p.Product).Include(p => p.Unit)
            .Where(p => p.SupplierId == supplierId)
            .OrderBy(p => p.ProductId).ThenByDescending(p => p.ValidFrom)
            .ToListAsync();
        return Ok(prices);
    }

    [HttpPost("{supplierId}/prices")]
    public async Task<IActionResult> AddPrice(int supplierId, [FromBody] SupplierPriceContract model)
    {
        model.SupplierId = supplierId;
        // Expire old active contract for same product/unit
        var old = await _db.SupplierPriceContracts.Where(p => p.SupplierId == supplierId && p.ProductId == model.ProductId && p.UnitId == model.UnitId && p.IsActive).ToListAsync();
        foreach (var o in old) { o.IsActive = false; o.ValidTo = DateTime.UtcNow; }
        model.IsActive = true;
        _db.SupplierPriceContracts.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    // Supplier Accounts
    [HttpGet("{supplierId}/account")]
    public async Task<IActionResult> GetAccount(int supplierId)
    {
        var supplier = await _db.WH_Suppliers.FindAsync(supplierId);
        if (supplier == null) return NotFound();
        var invoices = await _db.SupplierInvoices.Where(i => i.SupplierId == supplierId).OrderByDescending(i => i.InvoiceDate).ToListAsync();
        var payments = await _db.SupplierPayments.Where(p => p.SupplierId == supplierId).OrderByDescending(p => p.PaymentDate).ToListAsync();
        return Ok(new
        {
            SupplierId = supplierId, SupplierName = supplier.NameAr,
            CurrentBalance = supplier.CurrentBalance, CreditLimit = supplier.CreditLimit,
            TotalInvoiced = invoices.Sum(i => i.TotalAmount), TotalPaid = invoices.Sum(i => i.PaidAmount),
            TotalOutstanding = invoices.Where(i => !i.IsFullyPaid).Sum(i => i.TotalAmount - i.PaidAmount),
            Invoices = invoices, Payments = payments
        });
    }

    [HttpPost("{supplierId}/payments")]
    public async Task<IActionResult> AddPayment(int supplierId, [FromBody] SupplierPayment model)
    {
        var supplier = await _db.WH_Suppliers.FindAsync(supplierId);
        if (supplier == null) return NotFound();
        model.SupplierId = supplierId; model.PaymentDate = DateTime.UtcNow;
        var year = DateTime.Now.Year;
        var count = await _db.SupplierPayments.CountAsync() + 1;
        model.PaymentNumber = $"PAY-{year}-{count:D4}";
        supplier.CurrentBalance -= model.Amount;
        _db.SupplierPayments.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [AllowAnonymous]
    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookup()
    {
        var suppliers = await _db.WH_Suppliers.Where(s => s.IsActive)
            .OrderBy(s => s.NameAr)
            .Select(s => new {
                s.Id,
                Name = s.NameAr ?? s.NameEn,
                s.Code,
                s.Phone
            }).ToListAsync();
        return Ok(suppliers);
    }

    [AllowAnonymous]
    [HttpGet("price-contracts")]
    public async Task<IActionResult> GetAllPriceContracts()
    {
        var contracts = await _db.SupplierPriceContracts
            .Include(c => c.Supplier)
            .Include(c => c.Product)
            .Include(c => c.Unit)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.ValidFrom)
            .Select(c => new {
                c.Id,
                c.SupplierId,
                SupplierName = c.Supplier != null ? c.Supplier.NameAr : null,
                c.ProductId,
                ProductName = c.Product != null ? c.Product.NameAr : null,
                UnitName = c.Unit != null ? c.Unit.NameAr : null,
                UnitPrice = c.AgreedPrice,
                StartDate = (DateTime?)c.ValidFrom,
                EndDate = c.ValidTo,
                IsPreferred = true,
                c.IsActive
            }).ToListAsync();
        return Ok(contracts);
    }

    [AllowAnonymous]
    [HttpPost("price-contracts")]
    public async Task<IActionResult> CreatePriceContractGeneral([FromBody] SupplierPriceContract model)
    {
        model.ValidFrom = model.ValidFrom == default ? DateTime.UtcNow : model.ValidFrom;
        model.IsActive = true;
        _db.SupplierPriceContracts.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [AllowAnonymous]
    [HttpGet("{supplierId}/accounts")]
    public async Task<IActionResult> GetAccountsHistory(int supplierId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var invoices = await _db.SupplierInvoices.Where(i => i.SupplierId == supplierId).ToListAsync();
        var payments = await _db.SupplierPayments.Where(p => p.SupplierId == supplierId).ToListAsync();

        var list = new List<object>();
        decimal running = 0;

        foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
        {
            running += inv.TotalAmount;
            list.Add(new {
                Date = inv.InvoiceDate,
                TransactionType = "فاتورة مشتريات",
                Reference = inv.InvoiceNumber,
                Debit = inv.TotalAmount,
                Credit = 0m,
                RunningBalance = running
            });
        }

        foreach (var p in payments.OrderBy(p => p.PaymentDate))
        {
            running -= p.Amount;
            list.Add(new {
                Date = p.PaymentDate,
                TransactionType = "سند صرف / سداد",
                Reference = p.PaymentNumber,
                Debit = 0m,
                Credit = p.Amount,
                RunningBalance = running
            });
        }

        return Ok(list);
    }

public class SupplierInputDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? TaxNumber { get; set; }
    public string? CommercialRegister { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public int? PaymentTermsDays { get; set; }
    public PaymentTermType? DefaultPaymentTerm { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SupplierPriceContractInputDto>? PriceContracts { get; set; }
}

public class SupplierPriceContractInputDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPreferred { get; set; }
}

}
