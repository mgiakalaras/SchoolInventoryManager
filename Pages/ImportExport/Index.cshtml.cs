using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.ImportExport;

public class IndexModel : PageModel
{
    private const string CreateValue = "__create__";
    private const string DuplicateActionSkip = "skip";
    private const string DuplicateActionNew = "new";
    private const string DuplicateActionUpdate = "update";

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public string? ImportBatchId { get; set; }

    [BindProperty]
    public bool SplitTrackedBulkItems { get; set; } = true;

    [BindProperty]
    public bool SkipExistingItems { get; set; } = true;

    [BindProperty]
    public Dictionary<string, string> RoomDecisions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [BindProperty]
    public Dictionary<string, string> CategoryDecisions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [BindProperty]
    public Dictionary<string, string> RowActions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ImportResult? Result { get; set; }
    public ImportPreview? Preview { get; set; }

    public int RoomCount { get; set; }
    public int ItemCount { get; set; }
    public int CategoryCount { get; set; }

    public IList<Room> ExistingRooms { get; set; } = new List<Room>();
    public IList<InventoryCategory> ExistingCategories { get; set; } = new List<InventoryCategory>();

    public async Task OnGetAsync()
    {
        await LoadStatsAsync();
        await LoadReferenceListsAsync();
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        await LoadStatsAsync();
        await LoadReferenceListsAsync();

        if (UploadFile == null || UploadFile.Length == 0)
        {
            Result = ImportResult.Failed("Δεν ανέβηκε αρχείο.");
            return Page();
        }

        var extension = Path.GetExtension(UploadFile.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
        {
            Result = ImportResult.Failed("Για την ώρα η εισαγωγή δέχεται μόνο αρχείο Excel .xlsx.");
            return Page();
        }

        try
        {
            ImportBatchId = Guid.NewGuid().ToString("N");
            var importPath = GetImportBatchPath(ImportBatchId);
            Directory.CreateDirectory(Path.GetDirectoryName(importPath)!);

            await using (var output = System.IO.File.Create(importPath))
            await using (var input = UploadFile.OpenReadStream())
            {
                await input.CopyToAsync(output);
            }

            await using var stream = System.IO.File.OpenRead(importPath);
            var parse = ReadImportRows(stream);
            Preview = await BuildPreviewAsync(ImportBatchId, parse.Rows, parse.Errors, SplitTrackedBulkItems);
            return Page();
        }
        catch (Exception ex)
        {
            Result = ImportResult.Failed($"Η προεπισκόπηση απέτυχε: {ex.Message}");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmImportAsync()
    {
        await LoadStatsAsync();
        await LoadReferenceListsAsync();

        if (string.IsNullOrWhiteSpace(ImportBatchId))
        {
            Result = ImportResult.Failed("Δεν βρέθηκε προσωρινό αρχείο εισαγωγής. Ανέβασε ξανά το Excel.");
            return Page();
        }

        var importPath = GetImportBatchPath(ImportBatchId);
        if (!System.IO.File.Exists(importPath))
        {
            Result = ImportResult.Failed("Το προσωρινό αρχείο εισαγωγής δεν υπάρχει πια. Ανέβασε ξανά το Excel.");
            return Page();
        }

        try
        {
            await using var stream = System.IO.File.OpenRead(importPath);
            var parse = ReadImportRows(stream);
            Preview = await BuildPreviewAsync(ImportBatchId, parse.Rows, parse.Errors, SplitTrackedBulkItems);

            if (Preview.Errors.Any())
            {
                Result = ImportResult.Failed("Υπάρχουν σφάλματα στο Excel. Διόρθωσέ τα και ξανακάνε προεπισκόπηση.");
                return Page();
            }

            var missingRoomDecision = Preview.UnknownRooms.FirstOrDefault(x => !RoomDecisions.ContainsKey(x.Key));
            var missingCategoryDecision = Preview.UnknownCategories.FirstOrDefault(x => !CategoryDecisions.ContainsKey(x.Key));
            if (missingRoomDecision != null || missingCategoryDecision != null)
            {
                Result = ImportResult.Failed("Χρειάζεται επιλογή για κάθε άγνωστο χώρο και κάθε άγνωστη κατηγορία πριν την εισαγωγή.");
                return Page();
            }

            var result = new ImportResult();
            var rooms = await _db.Rooms.ToListAsync();
            var categories = await _db.InventoryCategories.ToListAsync();

            var roomLookup = BuildLookup(rooms, x => x.Name);
            var categoryLookup = BuildLookup(categories, x => x.Name);
            var roomsById = rooms.ToDictionary(x => x.Id);
            var categoriesById = categories.ToDictionary(x => x.Id);

            var nextRoomSortOrder = rooms.Any() ? rooms.Max(x => x.SortOrder) + 10 : 10;
            var nextCategorySortOrder = categories.Any() ? categories.Max(x => x.SortOrder) + 10 : 10;

            var existingItems = await _db.InventoryItems
                .Include(x => x.Room)
                .Include(x => x.InventoryCategory)
                .ToListAsync();

            var existingDuplicateCandidates = BuildExistingDuplicateCandidates(existingItems);
            var importedRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in parse.Rows)
            {
                var normalizedRoom = NormalizeLookup(row.RoomName);
                if (!roomLookup.TryGetValue(normalizedRoom, out var room))
                {
                    var decision = RoomDecisions.TryGetValue(normalizedRoom, out var selectedRoomDecision)
                        ? selectedRoomDecision
                        : CreateValue;

                    if (decision == CreateValue)
                    {
                        room = new Room
                        {
                            Name = row.RoomName.Trim(),
                            SortOrder = nextRoomSortOrder
                        };
                        nextRoomSortOrder += 10;
                        _db.Rooms.Add(room);
                        roomLookup[normalizedRoom] = room;
                        result.CreatedRooms++;
                    }
                    else if (int.TryParse(decision, NumberStyles.Integer, CultureInfo.InvariantCulture, out var roomId) && roomsById.TryGetValue(roomId, out var existingRoom))
                    {
                        room = existingRoom;
                        roomLookup[normalizedRoom] = room;
                    }
                    else
                    {
                        result.SkippedRows++;
                        result.Errors.Add($"Γραμμή {row.ExcelRow}: μη έγκυρη επιλογή χώρου.");
                        continue;
                    }
                }

                InventoryCategory? category = null;
                if (!string.IsNullOrWhiteSpace(row.CategoryName))
                {
                    var normalizedCategory = NormalizeLookup(row.CategoryName);
                    if (!categoryLookup.TryGetValue(normalizedCategory, out category))
                    {
                        var decision = CategoryDecisions.TryGetValue(normalizedCategory, out var selectedCategoryDecision)
                            ? selectedCategoryDecision
                            : CreateValue;

                        if (decision == CreateValue)
                        {
                            category = new InventoryCategory
                            {
                                Name = row.CategoryName.Trim(),
                                SortOrder = nextCategorySortOrder
                            };
                            nextCategorySortOrder += 10;
                            _db.InventoryCategories.Add(category);
                            categoryLookup[normalizedCategory] = category;
                            result.CreatedCategories++;
                        }
                        else if (int.TryParse(decision, NumberStyles.Integer, CultureInfo.InvariantCulture, out var categoryId) && categoriesById.TryGetValue(categoryId, out var existingCategory))
                        {
                            category = existingCategory;
                            categoryLookup[normalizedCategory] = category;
                        }
                        else
                        {
                            result.SkippedRows++;
                            result.Errors.Add($"Γραμμή {row.ExcelRow}: μη έγκυρη επιλογή κατηγορίας.");
                            continue;
                        }
                    }
                }

                var shouldSplit = SplitTrackedBulkItems && ShouldSplitTrackedItem(row) && row.Quantity > 1 && string.IsNullOrWhiteSpace(row.SerialNumber);
                var copies = shouldSplit ? row.Quantity : 1;
                var importedQuantity = shouldSplit ? 1 : row.Quantity;

                var rowDuplicateKey = BuildImportRowDuplicateKey(row, room.Name, category?.Name);
                var isDuplicateInsideFile = !importedRowKeys.Add(rowDuplicateKey);
                var itemDuplicateKeys = BuildImportDuplicateKeys(row, room.Name, category?.Name, importedQuantity).ToList();
                var existingConflict = FindImportConflict(itemDuplicateKeys, existingDuplicateCandidates);

                if (SkipExistingItems && (isDuplicateInsideFile || existingConflict != null))
                {
                    var rowActionKey = row.ExcelRow.ToString(CultureInfo.InvariantCulture);
                    var selectedAction = RowActions.TryGetValue(rowActionKey, out var action)
                        ? action
                        : DuplicateActionSkip;

                    if (selectedAction == DuplicateActionUpdate && existingConflict?.Item != null)
                    {
                        UpdateInventoryItemFromImport(existingConflict.Item, row, room, category, importedQuantity);
                        result.UpdatedItems++;
                        result.Warnings.Add($"Γραμμή {row.ExcelRow}: ενημερώθηκε υπάρχουσα εγγραφή ({BuildItemSummary(existingConflict.Item)}).");
                        continue;
                    }

                    if (selectedAction != DuplicateActionNew)
                    {
                        result.SkippedRows++;
                        result.DuplicateItemsSkipped += copies;

                        if (isDuplicateInsideFile)
                        {
                            result.DuplicateRowsInFile++;
                            result.Warnings.Add($"Γραμμή {row.ExcelRow}: αγνοήθηκε γιατί υπάρχει ήδη ίδια γραμμή μέσα στο Excel.");
                        }
                        else if (existingConflict != null)
                        {
                            result.Warnings.Add($"Γραμμή {row.ExcelRow}: αγνοήθηκε γιατί ταιριάζει με υπάρχουσα εγγραφή ({BuildItemSummary(existingConflict.Item)}).");
                        }

                        continue;
                    }
                }

                for (var copy = 1; copy <= copies; copy++)
                {
                    var item = new InventoryItem
                    {
                        Room = room,
                        InventoryCategory = category,
                        Name = row.ItemName.Trim(),
                        Quantity = importedQuantity,
                        Brand = NullIfWhiteSpace(row.Brand),
                        Model = NullIfWhiteSpace(row.Model),
                        SerialNumber = NullIfWhiteSpace(row.SerialNumber),
                        Description = NullIfWhiteSpace(row.Description),
                        Condition = row.Condition,
                        Notes = shouldSplit
                            ? AppendNote(row.Notes, $"Ανάλυση μαζικής εγγραφής από Excel. Αρχική ποσότητα: {row.Quantity}. Τεμάχιο {copy}/{copies}.")
                            : NullIfWhiteSpace(row.Notes),
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _db.InventoryItems.Add(item);
                    result.ImportedItems++;
                    if (shouldSplit)
                    {
                        result.SplitRowsCreated++;
                    }
                }

                foreach (var duplicateKey in itemDuplicateKeys)
                {
                    if (!existingDuplicateCandidates.ContainsKey(duplicateKey))
                    {
                        existingDuplicateCandidates[duplicateKey] = new ImportDuplicateCandidate(duplicateKey, null, "Εγγραφή που δημιουργήθηκε στην τρέχουσα εισαγωγή");
                    }
                }
            }

            await _db.SaveChangesAsync();
            TryDeleteImportBatch(ImportBatchId);

            Result = result;
            Preview = null;
            ImportBatchId = null;
            RoomDecisions.Clear();
            CategoryDecisions.Clear();

            await LoadStatsAsync();
            await LoadReferenceListsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            Result = ImportResult.Failed($"Η εισαγωγή απέτυχε: {ex.Message}");
            return Page();
        }
    }

    public IActionResult OnGetTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("InventoryImport");
        var headers = GetExportHeaders();

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        ws.Cell(2, 1).Value = "Γραφείο Καθηγητών";
        ws.Cell(2, 2).Value = "Η/Υ";
        ws.Cell(2, 3).Value = "Υπολογιστικό σύστημα";
        ws.Cell(2, 4).Value = 3;
        ws.Cell(2, 8).Value = "Λειτουργικό";
        ws.Cell(2, 10).Value = "Αν παραμείνει ποσότητα > 1 σε Η/Υ, tablet, οθόνες κλπ., η εισαγωγή μπορεί να το αναλύσει σε ξεχωριστές εγγραφές.";

        ws.Cell(3, 1).Value = "Γραφείο Καθηγητών";
        ws.Cell(3, 2).Value = "Εκτυπωτής";
        ws.Cell(3, 3).Value = "HP LaserJet P1505";
        ws.Cell(3, 4).Value = 1;
        ws.Cell(3, 8).Value = "Λειτουργικό";

        FormatWorksheet(ws, headers.Length);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "school_inventory_import_template.xlsx");
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        var items = await GetExportItemsAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("InventoryExport");
        var headers = GetExportHeaders();

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        foreach (var item in items)
        {
            WriteItemRow(ws, row++, item);
        }

        FormatWorksheet(ws, headers.Length);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"school_inventory_export_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var items = await GetExportItemsAsync();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", GetExportHeaders().Select(CsvEscape)));

        foreach (var item in items)
        {
            var values = new[]
            {
                item.Room?.Name ?? string.Empty,
                item.InventoryCategory?.Name ?? string.Empty,
                item.Name,
                item.Quantity.ToString(CultureInfo.InvariantCulture),
                item.Brand ?? string.Empty,
                item.Model ?? string.Empty,
                item.SerialNumber ?? string.Empty,
                item.Condition.GetDisplayName(),
                item.Description ?? string.Empty,
                item.Notes ?? string.Empty
            };

            sb.AppendLine(string.Join(";", values.Select(CsvEscape)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"school_inventory_export_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    private async Task LoadStatsAsync()
    {
        RoomCount = await _db.Rooms.CountAsync();
        CategoryCount = await _db.InventoryCategories.CountAsync();
        ItemCount = await _db.InventoryItems.CountAsync(x => x.IsActive);
    }

    private async Task LoadReferenceListsAsync()
    {
        ExistingRooms = await _db.Rooms.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        ExistingCategories = await _db.InventoryCategories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
    }

    private async Task<ImportPreview> BuildPreviewAsync(string batchId, IReadOnlyList<ImportRowData> rows, IReadOnlyList<string> parseErrors, bool splitTracked)
    {
        await LoadReferenceListsAsync();

        var roomLookup = BuildLookup(ExistingRooms, x => x.Name);
        var categoryLookup = BuildLookup(ExistingCategories, x => x.Name);

        var unknownRooms = rows
            .Select(x => x.RoomName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(NormalizeLookup)
            .Where(g => !roomLookup.ContainsKey(g.Key))
            .Select(g => new UnknownImportValue(
                g.Key,
                g.First(),
                FindSuggestedId(g.First(), ExistingRooms.Select(x => (x.Id, x.Name))).ToString(CultureInfo.InvariantCulture)))
            .OrderBy(x => x.Name)
            .ToList();

        var unknownCategories = rows
            .Select(x => x.CategoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => NormalizeLookup(x!))
            .Where(g => !categoryLookup.ContainsKey(g.Key))
            .Select(g => new UnknownImportValue(
                g.Key,
                g.First() ?? string.Empty,
                FindSuggestedId(g.First() ?? string.Empty, ExistingCategories.Select(x => (x.Id, x.Name))).ToString(CultureInfo.InvariantCulture)))
            .OrderBy(x => x.Name)
            .ToList();

        foreach (var item in unknownRooms.Where(x => x.SuggestedId == "0"))
        {
            item.SuggestedId = string.Empty;
        }

        foreach (var item in unknownCategories.Where(x => x.SuggestedId == "0"))
        {
            item.SuggestedId = string.Empty;
        }

        var existingItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .ToListAsync();

        var existingDuplicateCandidates = BuildExistingDuplicateCandidates(existingItems);
        var previewRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var previewRows = new List<ImportPreviewRow>();
        var estimatedImportedItems = 0;
        var estimatedNewItems = 0;
        var duplicateItemsEstimated = 0;
        var duplicateRowsInFile = 0;
        var potentialDuplicateRows = 0;

        foreach (var row in rows)
        {
            var willSplit = splitTracked && ShouldSplitTrackedItem(row) && row.Quantity > 1 && string.IsNullOrWhiteSpace(row.SerialNumber);
            var outputRows = willSplit ? row.Quantity : 1;
            var importedQuantity = willSplit ? 1 : row.Quantity;
            estimatedImportedItems += outputRows;

            var rowDuplicateKey = BuildImportRowDuplicateKey(row, row.RoomName, row.CategoryName);
            var isDuplicateInsideFile = !previewRowKeys.Add(rowDuplicateKey);
            var duplicateKeys = BuildImportDuplicateKeys(row, row.RoomName, row.CategoryName, importedQuantity).ToList();
            var existingConflict = FindImportConflict(duplicateKeys, existingDuplicateCandidates);
            var isExistingDuplicate = existingConflict != null;

            var willSkipAsDuplicate = SkipExistingItems && (isDuplicateInsideFile || isExistingDuplicate);
            var duplicateStatus = string.Empty;
            var existingSummary = string.Empty;
            int? existingItemId = null;
            var suggestedAction = DuplicateActionNew;

            if (willSkipAsDuplicate)
            {
                duplicateItemsEstimated += outputRows;
                suggestedAction = DuplicateActionSkip;

                if (isDuplicateInsideFile)
                {
                    duplicateRowsInFile++;
                    duplicateStatus = "Διπλή γραμμή στο Excel";
                }
                else if (existingConflict != null)
                {
                    potentialDuplicateRows++;
                    duplicateStatus = existingConflict.MatchDescription;
                    existingSummary = BuildItemSummary(existingConflict.Item);
                    existingItemId = existingConflict.Item?.Id;
                }
            }
            else
            {
                estimatedNewItems += outputRows;
            }

            if (previewRows.Count < 1000)
            {
                previewRows.Add(new ImportPreviewRow(
                    row.ExcelRow,
                    row.RoomName,
                    row.CategoryName,
                    row.ItemName,
                    row.Quantity,
                    row.Brand,
                    row.Model,
                    row.SerialNumber,
                    willSplit,
                    outputRows,
                    willSkipAsDuplicate,
                    duplicateStatus,
                    isExistingDuplicate,
                    existingItemId,
                    existingSummary,
                    suggestedAction));
            }
        }

        return new ImportPreview
        {
            BatchId = batchId,
            TotalRows = rows.Count + parseErrors.Count,
            ValidRows = rows.Count,
            EstimatedImportedItems = estimatedImportedItems,
            EstimatedNewItems = estimatedNewItems,
            DuplicateItemsEstimated = duplicateItemsEstimated,
            PotentialDuplicateRows = potentialDuplicateRows,
            DuplicateRowsInFile = duplicateRowsInFile,
            SplitRowsEstimated = estimatedImportedItems - rows.Count,
            Errors = parseErrors.ToList(),
            UnknownRooms = unknownRooms,
            UnknownCategories = unknownCategories,
            Rows = previewRows
        };
    }

    private async Task<List<InventoryItem>> GetExportItemsAsync()
    {
        return await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    private static string[] GetExportHeaders()
    {
        return new[]
        {
            "Χώρος",
            "Κατηγορία",
            "Αντικείμενο",
            "Ποσότητα",
            "Μάρκα",
            "Μοντέλο",
            "Serial Number",
            "Κατάσταση",
            "Περιγραφή",
            "Παρατηρήσεις"
        };
    }

    private static void WriteItemRow(IXLWorksheet ws, int row, InventoryItem item)
    {
        ws.Cell(row, 1).Value = item.Room?.Name ?? string.Empty;
        ws.Cell(row, 2).Value = item.InventoryCategory?.Name ?? string.Empty;
        ws.Cell(row, 3).Value = item.Name;
        ws.Cell(row, 4).Value = item.Quantity;
        ws.Cell(row, 5).Value = item.Brand ?? string.Empty;
        ws.Cell(row, 6).Value = item.Model ?? string.Empty;
        ws.Cell(row, 7).Value = item.SerialNumber ?? string.Empty;
        ws.Cell(row, 8).Value = item.Condition.GetDisplayName();
        ws.Cell(row, 9).Value = item.Description ?? string.Empty;
        ws.Cell(row, 10).Value = item.Notes ?? string.Empty;
    }

    private static void FormatWorksheet(IXLWorksheet ws, int columnCount)
    {
        var header = ws.Range(1, 1, 1, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("111827");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();
        ws.Column(9).Width = Math.Max(ws.Column(9).Width, 30);
        ws.Column(10).Width = Math.Max(ws.Column(10).Width, 30);
    }

    private ImportParseResult ReadImportRows(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        var rows = new List<ImportRowData>();
        var errors = new List<string>();

        if (worksheet == null)
        {
            errors.Add("Το Excel δεν έχει φύλλο εργασίας.");
            return new ImportParseResult(rows, errors);
        }

        var headerMap = BuildHeaderMap(worksheet);
        var roomColumn = FindColumn(headerMap, "ΧΩΡΟΣ", "ΑΙΘΟΥΣΑ", "ROOM", "SPACE");
        var itemColumn = FindColumn(headerMap, "ΑΝΤΙΚΕΙΜΕΝΟ", "ΕΙΔΟΣ", "ITEM", "NAME", "EQUIPMENT");

        if (roomColumn == null || itemColumn == null)
        {
            errors.Add("Λείπουν υποχρεωτικές στήλες. Χρειάζονται τουλάχιστον: Χώρος και Αντικείμενο.");
            return new ImportParseResult(rows, errors);
        }

        var categoryColumn = FindColumn(headerMap, "ΚΑΤΗΓΟΡΙΑ", "CATEGORY");
        var quantityColumn = FindColumn(headerMap, "ΠΟΣΟΤΗΤΑ", "QUANTITY", "QTY");
        var brandColumn = FindColumn(headerMap, "ΜΑΡΚΑ", "BRAND");
        var modelColumn = FindColumn(headerMap, "ΜΟΝΤΕΛΟ", "MODEL");
        var serialColumn = FindColumn(headerMap, "SERIALNUMBER", "SERIAL", "SN", "S/N");
        var descriptionColumn = FindColumn(headerMap, "ΠΕΡΙΓΡΑΦΗ", "DESCRIPTION");
        var conditionColumn = FindColumn(headerMap, "ΚΑΤΑΣΤΑΣΗ", "CONDITION");
        var notesColumn = FindColumn(headerMap, "ΠΑΡΑΤΗΡΗΣΕΙΣ", "NOTES");

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            var roomName = GetCellText(worksheet, row, roomColumn.Value);
            var itemName = GetCellText(worksheet, row, itemColumn.Value);

            if (string.IsNullOrWhiteSpace(roomName) && string.IsNullOrWhiteSpace(itemName))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(itemName))
            {
                errors.Add($"Γραμμή {row}: χρειάζεται και Χώρος και Αντικείμενο.");
                continue;
            }

            var quantity = ParseQuantity(GetCellText(worksheet, row, quantityColumn));
            if (quantity <= 0)
            {
                errors.Add($"Γραμμή {row}: μη έγκυρη ποσότητα.");
                continue;
            }

            rows.Add(new ImportRowData(
                row,
                roomName.Trim(),
                NullIfWhiteSpace(GetCellText(worksheet, row, categoryColumn)),
                itemName.Trim(),
                quantity,
                NullIfWhiteSpace(GetCellText(worksheet, row, brandColumn)),
                NullIfWhiteSpace(GetCellText(worksheet, row, modelColumn)),
                NullIfWhiteSpace(GetCellText(worksheet, row, serialColumn)),
                ParseCondition(GetCellText(worksheet, row, conditionColumn)),
                NullIfWhiteSpace(GetCellText(worksheet, row, descriptionColumn)),
                NullIfWhiteSpace(GetCellText(worksheet, row, notesColumn))));
        }

        return new ImportParseResult(rows, errors);
    }

    private static Dictionary<string, T> BuildLookup<T>(IEnumerable<T> values, Func<T, string> nameSelector)
    {
        return values
            .GroupBy(x => NormalizeLookup(nameSelector(x)))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var col = 1; col <= lastColumn; col++)
        {
            var header = NormalizeHeader(worksheet.Cell(1, col).GetString());
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
            {
                map[header] = col;
            }
        }

        return map;
    }

    private static int? FindColumn(Dictionary<string, int> headerMap, params string[] names)
    {
        foreach (var name in names)
        {
            var normalized = NormalizeHeader(name);
            if (headerMap.TryGetValue(normalized, out var col))
            {
                return col;
            }
        }

        return null;
    }

    private static string GetCellText(IXLWorksheet worksheet, int row, int? column)
    {
        if (column == null)
        {
            return string.Empty;
        }

        return worksheet.Cell(row, column.Value).GetString().Trim();
    }

    private static int ParseQuantity(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 1;
        }

        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var invariantResult))
        {
            return invariantResult;
        }

        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var currentResult))
        {
            return currentResult;
        }

        return 0;
    }

    private static EquipmentCondition ParseCondition(string value)
    {
        var normalized = NormalizeHeader(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return EquipmentCondition.Unknown;
        }

        if (normalized.Contains("ΛΕΙΤΟΥΡΓ") || normalized.Contains("WORKING") || normalized.Contains("OK"))
        {
            return EquipmentCondition.Working;
        }

        if (normalized.Contains("ΕΛΕΓΧ") || normalized.Contains("CHECK"))
        {
            return EquipmentCondition.NeedsCheck;
        }

        if (normalized.Contains("ΧΑΛ") || normalized.Contains("BROKEN") || normalized.Contains("DAMAGE"))
        {
            return EquipmentCondition.Broken;
        }

        if (normalized.Contains("ΑΠΟΣΥΡ") || normalized.Contains("WITHDRAW") || normalized.Contains("SCRAP"))
        {
            return EquipmentCondition.ToWithdraw;
        }

        if (normalized.Contains("ΑΠΟΘ") || normalized.Contains("STOR"))
        {
            return EquipmentCondition.Stored;
        }

        return EquipmentCondition.Unknown;
    }

    private static bool ShouldSplitTrackedItem(ImportRowData row)
    {
        var value = NormalizeHeader($"{row.CategoryName} {row.ItemName}");
        var tokens = new[]
        {
            "ΗΥ",
            "ΥΠΟΛΟΓΙΣ",
            "COMPUTER",
            "DESKTOP",
            "LAPTOP",
            "NOTEBOOK",
            "PC",
            "TABLET",
            "TABLETS",
            "ΤΑΜΠΛΕΤ",
            "ΟΘΟΝ",
            "MONITOR",
            "DISPLAY"
        };

        return tokens.Any(value.Contains);
    }

    private static int FindSuggestedId(string incomingName, IEnumerable<(int Id, string Name)> existingValues)
    {
        var incoming = NormalizeHeader(incomingName);
        var incomingSingular = TrimCommonPluralEnding(incoming);

        foreach (var value in existingValues)
        {
            var existing = NormalizeHeader(value.Name);
            var existingSingular = TrimCommonPluralEnding(existing);
            if (incoming == existing || incomingSingular == existingSingular || incoming.Contains(existingSingular) || existing.Contains(incomingSingular))
            {
                return value.Id;
            }
        }

        return 0;
    }

    private static string TrimCommonPluralEnding(string value)
    {
        if (value.Length <= 3)
        {
            return value;
        }

        if (value.EndsWith("S", StringComparison.OrdinalIgnoreCase) || value.EndsWith("Σ", StringComparison.OrdinalIgnoreCase))
        {
            return value[..^1];
        }

        return value;
    }

    private static Dictionary<string, ImportDuplicateCandidate> BuildExistingDuplicateCandidates(IEnumerable<InventoryItem> items)
    {
        var candidates = new Dictionary<string, ImportDuplicateCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            foreach (var candidate in BuildExistingDuplicateCandidates(item))
            {
                if (!candidates.ContainsKey(candidate.Key))
                {
                    candidates[candidate.Key] = candidate;
                }
            }
        }

        return candidates;
    }

    private static IEnumerable<ImportDuplicateCandidate> BuildExistingDuplicateCandidates(InventoryItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.SerialNumber))
        {
            yield return new ImportDuplicateCandidate(
                BuildSerialDuplicateKey(item.SerialNumber),
                item,
                "Ίδιο serial number");
        }

        yield return new ImportDuplicateCandidate(
            BuildDuplicateDetailKey(
                item.Room?.Name,
                item.InventoryCategory?.Name,
                item.Name,
                item.Brand,
                item.Model,
                item.SerialNumber,
                item.Condition,
                item.Description,
                item.Quantity),
            item,
            "Ίδια αναλυτικά στοιχεία");

        yield return new ImportDuplicateCandidate(
            BuildLooseDuplicateKey(
                item.Room?.Name,
                item.InventoryCategory?.Name,
                item.Name,
                item.Brand,
                item.Model),
            item,
            "Ίδιος χώρος / κατηγορία / αντικείμενο / μάρκα / μοντέλο");
    }

    private static ImportDuplicateCandidate? FindImportConflict(
        IEnumerable<string> duplicateKeys,
        IReadOnlyDictionary<string, ImportDuplicateCandidate> existingDuplicateCandidates)
    {
        foreach (var key in duplicateKeys)
        {
            if (existingDuplicateCandidates.TryGetValue(key, out var candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void UpdateInventoryItemFromImport(
        InventoryItem existingItem,
        ImportRowData row,
        Room room,
        InventoryCategory? category,
        int importedQuantity)
    {
        existingItem.Room = room;
        existingItem.RoomId = room.Id;

        if (category != null)
        {
            existingItem.InventoryCategory = category;
            existingItem.InventoryCategoryId = category.Id;
        }

        existingItem.Name = row.ItemName.Trim();
        existingItem.Quantity = importedQuantity;
        existingItem.Condition = row.Condition;

        existingItem.Brand = NullIfWhiteSpace(row.Brand) ?? existingItem.Brand;
        existingItem.Model = NullIfWhiteSpace(row.Model) ?? existingItem.Model;
        existingItem.SerialNumber = NullIfWhiteSpace(row.SerialNumber) ?? existingItem.SerialNumber;
        existingItem.Description = NullIfWhiteSpace(row.Description) ?? existingItem.Description;
        existingItem.Notes = MergeNotes(existingItem.Notes, row.Notes);
        existingItem.UpdatedAt = DateTime.Now;
    }

    private static string BuildItemSummary(InventoryItem? item)
    {
        if (item == null)
        {
            return "εγγραφή από την τρέχουσα εισαγωγή";
        }

        var parts = new[]
        {
            item.Room?.Name,
            item.InventoryCategory?.Name,
            item.Name,
            item.Brand,
            item.Model,
            string.IsNullOrWhiteSpace(item.SerialNumber) ? null : $"SN: {item.SerialNumber}",
            item.IsActive ? null : "ανενεργό/ιστορικό"
        };

        return string.Join(" · ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string? MergeNotes(string? existingNotes, string? incomingNotes)
    {
        var incoming = NullIfWhiteSpace(incomingNotes);
        if (incoming == null)
        {
            return existingNotes;
        }

        if (string.IsNullOrWhiteSpace(existingNotes))
        {
            return incoming;
        }

        if (existingNotes.Contains(incoming, StringComparison.OrdinalIgnoreCase))
        {
            return existingNotes;
        }

        return $"{existingNotes.Trim()} | Import update: {incoming}";
    }

    private static IEnumerable<string> BuildImportDuplicateKeys(
        ImportRowData row,
        string? roomName,
        string? categoryName,
        int importedQuantity)
    {
        // 1) Serial number: strongest match when it exists.
        if (!string.IsNullOrWhiteSpace(row.SerialNumber))
        {
            yield return BuildSerialDuplicateKey(row.SerialNumber);
        }

        // 2) Full detail fingerprint: catches exact re-imports.
        yield return BuildDuplicateDetailKey(
            roomName,
            categoryName,
            row.ItemName,
            row.Brand,
            row.Model,
            row.SerialNumber,
            row.Condition,
            row.Description,
            importedQuantity);

        // 3) Loose fingerprint: catches devices that were previously imported
        // without serial and later re-imported with extra details.
        yield return BuildLooseDuplicateKey(
            roomName,
            categoryName,
            row.ItemName,
            row.Brand,
            row.Model);
    }

    private static string BuildImportRowDuplicateKey(ImportRowData row, string? roomName, string? categoryName)
    {
        return BuildDuplicateDetailKey(
            roomName,
            categoryName,
            row.ItemName,
            row.Brand,
            row.Model,
            row.SerialNumber,
            row.Condition,
            row.Description,
            row.Quantity);
    }

    private static string BuildSerialDuplicateKey(string? serialNumber)
    {
        return $"SERIAL|{NormalizeHeader(serialNumber)}";
    }

    private static string BuildLooseDuplicateKey(
        string? roomName,
        string? categoryName,
        string itemName,
        string? brand,
        string? model)
    {
        var parts = new[]
        {
            "LOOSE",
            NormalizeHeader(roomName),
            NormalizeHeader(categoryName),
            NormalizeHeader(itemName),
            NormalizeHeader(brand),
            NormalizeHeader(model)
        };

        return string.Join("|", parts);
    }

    private static string BuildDuplicateDetailKey(
        string? roomName,
        string? categoryName,
        string itemName,
        string? brand,
        string? model,
        string? serialNumber,
        EquipmentCondition condition,
        string? description,
        int quantity)
    {
        var parts = new[]
        {
            "DETAIL",
            NormalizeHeader(roomName),
            NormalizeHeader(categoryName),
            NormalizeHeader(itemName),
            NormalizeHeader(brand),
            NormalizeHeader(model),
            NormalizeHeader(serialNumber),
            ((int)condition).ToString(CultureInfo.InvariantCulture),
            NormalizeHeader(description),
            quantity.ToString(CultureInfo.InvariantCulture)
        };

        return string.Join("|", parts);
    }

    private string GetImportBatchPath(string batchId)
    {
        var safeBatchId = new string(batchId.Where(char.IsLetterOrDigit).ToArray());
        return Path.Combine(_environment.ContentRootPath, "App_Data", "imports", $"{safeBatchId}.xlsx");
    }

    private void TryDeleteImportBatch(string batchId)
    {
        try
        {
            var path = GetImportBatchPath(batchId);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
            // Δεν μπλοκάρουμε την εισαγωγή αν δεν σβηστεί το προσωρινό αρχείο.
        }
    }

    private static string? AppendNote(string? existing, string extra)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return extra;
        }

        return $"{existing.Trim()} | {extra}";
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeLookup(string value)
    {
        return NormalizeHeader(value);
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var formD = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToUpperInvariant(ch));
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CsvEscape(string value)
    {
        value ??= string.Empty;
        value = value.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        if (value.Contains(';') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public string CreateDecisionValue => CreateValue;
    public string DuplicateSkipValue => DuplicateActionSkip;
    public string DuplicateNewValue => DuplicateActionNew;
    public string DuplicateUpdateValue => DuplicateActionUpdate;
}

public class ImportResult
{
    public int ImportedItems { get; set; }
    public int CreatedRooms { get; set; }
    public int CreatedCategories { get; set; }
    public int SplitRowsCreated { get; set; }
    public int UpdatedItems { get; set; }
    public int SkippedRows { get; set; }
    public int DuplicateItemsSkipped { get; set; }
    public int DuplicateRowsInFile { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public bool IsSuccess => !Errors.Any() || ImportedItems > 0;
    public bool HasErrors => Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public static ImportResult Failed(string message)
    {
        return new ImportResult
        {
            Errors = new List<string> { message }
        };
    }
}

public class ImportPreview
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int EstimatedImportedItems { get; set; }
    public int EstimatedNewItems { get; set; }
    public int DuplicateItemsEstimated { get; set; }
    public int PotentialDuplicateRows { get; set; }
    public int DuplicateRowsInFile { get; set; }
    public int SplitRowsEstimated { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<UnknownImportValue> UnknownRooms { get; set; } = new();
    public List<UnknownImportValue> UnknownCategories { get; set; } = new();
    public List<ImportPreviewRow> Rows { get; set; } = new();
    public bool NeedsDecisions => UnknownRooms.Any() || UnknownCategories.Any();
}

public class UnknownImportValue
{
    public UnknownImportValue(string key, string name, string suggestedId)
    {
        Key = key;
        Name = name;
        SuggestedId = suggestedId;
    }

    public string Key { get; set; }
    public string Name { get; set; }
    public string SuggestedId { get; set; }
}

public record ImportPreviewRow(
    int ExcelRow,
    string RoomName,
    string? CategoryName,
    string ItemName,
    int Quantity,
    string? Brand,
    string? Model,
    string? SerialNumber,
    bool WillSplit,
    int OutputRows,
    bool WillSkipAsDuplicate,
    string DuplicateStatus,
    bool HasExistingConflict,
    int? ExistingItemId,
    string ExistingItemSummary,
    string SuggestedAction);

public record ImportDuplicateCandidate(
    string Key,
    InventoryItem? Item,
    string MatchDescription);

public record ImportParseResult(IReadOnlyList<ImportRowData> Rows, IReadOnlyList<string> Errors);

public record ImportRowData(
    int ExcelRow,
    string RoomName,
    string? CategoryName,
    string ItemName,
    int Quantity,
    string? Brand,
    string? Model,
    string? SerialNumber,
    EquipmentCondition Condition,
    string? Description,
    string? Notes);
