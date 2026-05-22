using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.SpareParts;

public class PrintModel : PageModel
{
    private readonly AppDbContext _db;

    public PrintModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool LowStockOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public IList<SparePartStock> Parts { get; set; } = new List<SparePartStock>();

    public int ActivePartRows { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockRows { get; set; }
    public int InactiveRows { get; set; }

    public string PrintDateText { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

    public string FilterText
    {
        get
        {
            var filters = new List<string>();

            filters.Add(string.IsNullOrWhiteSpace(TypeFilter)
                ? "Όλοι οι τύποι"
                : SparePartStock.GetPartTypeLabel(TypeFilter));

            if (LowStockOnly)
            {
                filters.Add("Μόνο χαμηλό απόθεμα");
            }

            if (ShowInactive)
            {
                filters.Add("Με αρχειοθετημένα");
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                filters.Add($"Αναζήτηση: {Search}");
            }

            return string.Join(" · ", filters);
        }
    }

    public async Task OnGetAsync()
    {
        ActivePartRows = await _db.SparePartStocks.CountAsync(x => x.IsActive);
        TotalQuantity = await _db.SparePartStocks
            .Where(x => x.IsActive)
            .SumAsync(x => x.Quantity);
        LowStockRows = await _db.SparePartStocks
            .CountAsync(x => x.IsActive && x.MinimumStock > 0 && x.Quantity <= x.MinimumStock);
        InactiveRows = await _db.SparePartStocks.CountAsync(x => !x.IsActive);

        var query = _db.SparePartStocks.AsQueryable();

        if (!ShowInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(TypeFilter))
        {
            query = query.Where(x => x.PartType == TypeFilter);
        }

        if (LowStockOnly)
        {
            query = query.Where(x => x.MinimumStock > 0 && x.Quantity <= x.MinimumStock);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Manufacturer != null && x.Manufacturer.Contains(term)) ||
                (x.ModelName != null && x.ModelName.Contains(term)) ||
                (x.Specification != null && x.Specification.Contains(term)) ||
                (x.StorageLocation != null && x.StorageLocation.Contains(term)) ||
                (x.CompatibleWith != null && x.CompatibleWith.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)));
        }

        Parts = await query
            .AsNoTracking()
            .OrderBy(x => x.PartType)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Manufacturer)
            .ThenBy(x => x.ModelName)
            .ToListAsync();
    }
}
