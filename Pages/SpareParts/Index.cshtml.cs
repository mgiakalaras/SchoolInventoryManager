using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.SpareParts;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
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

    [BindProperty]
    public SparePartStock NewPart { get; set; } = new()
    {
        PartType = "RAM",
        Quantity = 1,
        MinimumStock = 0,
        Condition = "Διαθέσιμο"
    };

    public IList<SparePartStock> Parts { get; set; } = new List<SparePartStock>();

    public IReadOnlyList<string> PartTypes => SparePartStock.PartTypes;

    public int ActivePartRows { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockRows { get; set; }
    public int InactiveRows { get; set; }

    public async Task OnGetAsync()
    {
        await LoadPartsAsync();
    }


    public async Task<IActionResult> OnGetExportAsync()
    {
        await LoadPartsAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Απόθεμα");

        var totalColumns = 12;

        worksheet.Cell(1, 1).Value = "School Inventory Manager";
        worksheet.Range(1, 1, 1, totalColumns).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0F766E");
        worksheet.Cell(1, 1).Style.Font.FontSize = 11;

        worksheet.Cell(2, 1).Value = "Αναφορά αποθέματος ανταλλακτικών / αναλώσιμων";
        worksheet.Range(2, 1, 2, totalColumns).Merge();
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 18;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#111827");

        worksheet.Cell(3, 1).Value = "Ημερομηνία εξαγωγής";
        worksheet.Cell(3, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        worksheet.Cell(3, 4).Value = "Φίλτρα";
        worksheet.Cell(3, 5).Value =
            (string.IsNullOrWhiteSpace(TypeFilter) ? "Όλοι οι τύποι" : SparePartStock.GetPartTypeLabel(TypeFilter)) +
            (LowStockOnly ? " | Μόνο χαμηλό απόθεμα" : string.Empty) +
            (ShowInactive ? " | Με αρχειοθετημένα" : string.Empty) +
            (string.IsNullOrWhiteSpace(Search) ? string.Empty : $" | Αναζήτηση: {Search}");

        worksheet.Range(3, 1, 3, totalColumns).Style.Font.FontColor = XLColor.FromHtml("#374151");
        worksheet.Range(3, 1, 3, totalColumns).Style.Font.FontSize = 10;

        var summaryRow = 5;
        var summaryData = new[]
        {
            ("Ενεργές εγγραφές", ActivePartRows),
            ("Συνολική ποσότητα", TotalQuantity),
            ("Χαμηλό απόθεμα", LowStockRows),
            ("Αρχειοθετημένα", InactiveRows)
        };

        for (var i = 0; i < summaryData.Length; i++)
        {
            var startColumn = 1 + (i * 2);
            worksheet.Cell(summaryRow, startColumn).Value = summaryData[i].Item1;
            worksheet.Cell(summaryRow, startColumn + 1).Value = summaryData[i].Item2;

            var block = worksheet.Range(summaryRow, startColumn, summaryRow, startColumn + 1);
            block.Style.Fill.BackgroundColor = XLColor.FromHtml(i == 2 ? "#FEF3C7" : "#E0F2FE");
            block.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            block.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            block.Style.Font.Bold = true;
            worksheet.Cell(summaryRow, startColumn + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var headerRow = 7;
        var headers = new[]
        {
            "Α/Α",
            "Τύπος",
            "Ονομασία",
            "Κατασκευαστής",
            "Μοντέλο",
            "Προδιαγραφή",
            "Ποσότητα",
            "Ελάχιστο",
            "Κατάσταση",
            "Θέση αποθήκευσης",
            "Συμβατότητα",
            "Σημειώσεις"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(headerRow, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(headerRow, 1, headerRow, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var currentRow = headerRow + 1;
        var counter = 1;

        foreach (var part in Parts)
        {
            worksheet.Cell(currentRow, 1).Value = counter;
            worksheet.Cell(currentRow, 2).Value = SparePartStock.GetPartTypeLabel(part.PartType);
            worksheet.Cell(currentRow, 3).Value = part.Name;
            worksheet.Cell(currentRow, 4).Value = part.Manufacturer;
            worksheet.Cell(currentRow, 5).Value = part.ModelName;
            worksheet.Cell(currentRow, 6).Value = part.Specification;
            worksheet.Cell(currentRow, 7).Value = part.Quantity;
            worksheet.Cell(currentRow, 8).Value = part.MinimumStock;
            worksheet.Cell(currentRow, 9).Value = part.IsActive ? part.Condition : "Αρχειοθετημένο";
            worksheet.Cell(currentRow, 10).Value = part.StorageLocation;
            worksheet.Cell(currentRow, 11).Value = part.CompatibleWith;
            worksheet.Cell(currentRow, 12).Value = part.Notes;

            var rowRange = worksheet.Range(currentRow, 1, currentRow, headers.Length);

            rowRange.Style.Fill.BackgroundColor = part.IsLowStock
                ? XLColor.FromHtml("#FFF7ED")
                : XLColor.FromHtml(counter % 2 == 0 ? "#F8FAFC" : "#FFFFFF");

            if (part.IsLowStock)
            {
                worksheet.Cell(currentRow, 7).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 7).Style.Font.FontColor = XLColor.FromHtml("#92400E");
            }

            if (!part.IsActive)
            {
                rowRange.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            }

            currentRow++;
            counter++;
        }

        var lastDataRow = Math.Max(headerRow, currentRow - 1);
        var usedRange = worksheet.Range(headerRow, 1, lastDataRow, headers.Length);

        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
        usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CBD5E1");
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        usedRange.Style.Alignment.WrapText = true;

        worksheet.Column(1).Width = 6;
        worksheet.Column(2).Width = 24;
        worksheet.Column(3).Width = 32;
        worksheet.Column(4).Width = 18;
        worksheet.Column(5).Width = 20;
        worksheet.Column(6).Width = 34;
        worksheet.Column(7).Width = 12;
        worksheet.Column(8).Width = 12;
        worksheet.Column(9).Width = 18;
        worksheet.Column(10).Width = 24;
        worksheet.Column(11).Width = 36;
        worksheet.Column(12).Width = 38;

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Range(headerRow, 1, headerRow, headers.Length).SetAutoFilter();

        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.PageSetup.Margins.Top = 0.45;
        worksheet.PageSetup.Margins.Bottom = 0.45;
        worksheet.PageSetup.Margins.Left = 0.25;
        worksheet.PageSetup.Margins.Right = 0.25;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"spare-parts-stock-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPart.PartType))
        {
            ModelState.AddModelError("NewPart.PartType", "Συμπλήρωσε τύπο.");
        }

        if (string.IsNullOrWhiteSpace(NewPart.Name))
        {
            ModelState.AddModelError("NewPart.Name", "Συμπλήρωσε ονομασία.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPartsAsync();
            return Page();
        }

        NewPart.PartType = NewPart.PartType.Trim();
        NewPart.Name = NewPart.Name.Trim();
        NewPart.Manufacturer = NormalizeOptional(NewPart.Manufacturer);
        NewPart.ModelName = NormalizeOptional(NewPart.ModelName);
        NewPart.Specification = NormalizeOptional(NewPart.Specification);
        NewPart.Condition = NormalizeOptional(NewPart.Condition) ?? "Διαθέσιμο";
        NewPart.StorageLocation = NormalizeOptional(NewPart.StorageLocation);
        NewPart.CompatibleWith = NormalizeOptional(NewPart.CompatibleWith);
        NewPart.Notes = NormalizeOptional(NewPart.Notes);
        NewPart.CreatedAt = DateTime.Now;
        NewPart.UpdatedAt = DateTime.Now;
        NewPart.IsActive = true;

        _db.SparePartStocks.Add(NewPart);
        await _db.SaveChangesAsync();

        TempData["SparePartMessage"] = "Το ανταλλακτικό/αναλώσιμο προστέθηκε στο απόθεμα.";

        return RedirectToPage("./Index", new { typeFilter = NewPart.PartType });
    }

    public async Task<IActionResult> OnPostAdjustAsync(int id, int delta, string? typeFilter, string? search, bool lowStockOnly, bool showInactive)
    {
        var part = await _db.SparePartStocks.FirstOrDefaultAsync(x => x.Id == id);

        if (part == null)
        {
            return NotFound();
        }

        part.Quantity = Math.Max(0, part.Quantity + delta);
        part.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["SparePartMessage"] = delta > 0
            ? "Η ποσότητα αυξήθηκε."
            : "Η ποσότητα μειώθηκε.";

        return RedirectToPage("./Index", new
        {
            typeFilter,
            search,
            lowStockOnly,
            showInactive
        });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string? typeFilter, string? search, bool lowStockOnly, bool showInactive)
    {
        var part = await _db.SparePartStocks.FirstOrDefaultAsync(x => x.Id == id);

        if (part == null)
        {
            return NotFound();
        }

        part.IsActive = !part.IsActive;
        part.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["SparePartMessage"] = part.IsActive
            ? "Το ανταλλακτικό ενεργοποιήθηκε."
            : "Το ανταλλακτικό αρχειοθετήθηκε.";

        return RedirectToPage("./Index", new
        {
            typeFilter,
            search,
            lowStockOnly,
            showInactive
        });
    }

    private async Task LoadPartsAsync()
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
            .OrderBy(x => x.PartType)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Manufacturer)
            .ThenBy(x => x.ModelName)
            .ToListAsync();
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
