using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

[Authorize]
[ApiController]
[Route("api/warehouse/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;
    public SuppliersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        var query = _db.WH_Suppliers.AsQueryable();
        if (activeOnly == true) query = query.Where(s => s.IsActive);
        return Ok(await query.OrderBy(s => s.NameAr).Select(s => new
        {
            s.Id, s.Code, s.NameAr, s.NameEn, s.Phone, s.Email, s.Address,
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
        return Ok(s);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Supplier model)
    {
        if (await _db.WH_Suppliers.AnyAsync(s => s.Code == model.Code))
            return BadRequest(new { message = "كود المورد موجود مسبقاً" });
        model.CreatedAt = DateTime.UtcNow;
        _db.WH_Suppliers.Add(model);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Supplier model)
    {
        var existing = await _db.WH_Suppliers.FindAsync(id);
        if (existing == null) return NotFound();
        existing.NameAr = model.NameAr; existing.NameEn = model.NameEn; existing.Phone = model.Phone;
        existing.Email = model.Email; existing.Address = model.Address; existing.TaxNumber = model.TaxNumber;
        existing.CommercialRegister = model.CommercialRegister; existing.BankName = model.BankName;
        existing.Iban = model.Iban; existing.DefaultPaymentTerm = model.DefaultPaymentTerm;
        existing.CreditLimit = model.CreditLimit; existing.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        return Ok(existing);
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
}
