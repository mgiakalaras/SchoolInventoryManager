using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class FolderCreateRoomModel : PageModel
{
    private const string FirstInventoryMarker = "[AuditMode:FirstInventory]";

    private readonly AppDbContext _db;

    public FolderCreateRoomModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnPostAsync(int folderId)
    {
        var request = await ReadRequestAsync();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Συμπλήρωσε όνομα χώρου."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
            .FirstOrDefaultAsync(x => x.Id == folderId);

        if (folder == null)
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Ο φάκελος απογραφής δεν βρέθηκε."
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        if (folder.IsFinalized)
        {
            return new JsonResult(new
            {
                ok = false,
                locked = true,
                message = "Ο φάκελος είναι οριστικοποιημένος."
            })
            {
                StatusCode = StatusCodes.Status423Locked
            };
        }

        var now = DateTime.Now;
        var cleanedName = request.Name.Trim();
        var normalizedName = Normalize(cleanedName);

        var rooms = await _db.Rooms.ToListAsync();
        var room = rooms.FirstOrDefault(x => Normalize(x.Name) == normalizedName);

        var createdRoom = false;

        if (room == null)
        {
            var nextSortOrder = rooms.Any()
                ? rooms.Max(x => x.SortOrder) + 10
                : 10;

            room = new Room
            {
                Name = cleanedName,
                SortOrder = request.SortOrder ?? nextSortOrder
            };

            _db.Rooms.Add(room);
            createdRoom = true;

            await _db.SaveChangesAsync();
        }

        var existingSession = folder.RoomSessions
            .FirstOrDefault(x =>
                (x.RoomId.HasValue && x.RoomId.Value == room.Id) ||
                Normalize(x.RoomNameSnapshot) == normalizedName);

        if (existingSession != null)
        {
            return new JsonResult(new
            {
                ok = true,
                createdRoom,
                createdSession = false,
                alreadyExists = true,
                message = "Ο χώρος υπάρχει ήδη στον φάκελο απογραφής.",
                room = new
                {
                    room.Id,
                    room.Name,
                    room.SortOrder
                },
                session = ToSessionDto(existingSession)
            });
        }

        var session = new InventoryAuditRoomSession
        {
            InventoryAuditFolderId = folder.Id,
            RoomId = room.Id,
            RoomNameSnapshot = room.Name,
            ExpectedItemsCount = 0,
            FoundItemsCount = 0,
            MissingItemsCount = 0,
            WrongRoomItemsCount = 0,
            UnknownItemsCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.InventoryAuditRoomSessions.Add(session);

        folder.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return new JsonResult(new
        {
            ok = true,
            createdRoom,
            createdSession = true,
            alreadyExists = false,
            isFirstInventory = IsFirstInventory(folder),
            message = createdRoom
                ? "Ο νέος χώρος δημιουργήθηκε και προστέθηκε στον φάκελο."
                : "Ο υπάρχων χώρος προστέθηκε στον φάκελο.",
            room = new
            {
                room.Id,
                room.Name,
                room.SortOrder
            },
            session = ToSessionDto(session)
        });
    }

    private async Task<CreateRoomRequest> ReadRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            return new CreateRoomRequest
            {
                Name = Request.Form["name"].FirstOrDefault(),
                SortOrder = TryParseInt(Request.Form["sortOrder"].FirstOrDefault())
            };
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<CreateRoomRequest>(
                Request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return request ?? new CreateRoomRequest();
        }
        catch
        {
            return new CreateRoomRequest();
        }
    }

    private static object ToSessionDto(InventoryAuditRoomSession session)
    {
        return new
        {
            session.Id,
            session.InventoryAuditFolderId,
            session.RoomId,
            roomName = session.RoomNameSnapshot,
            session.ExpectedItemsCount,
            session.FoundItemsCount,
            session.MissingItemsCount,
            session.WrongRoomItemsCount,
            session.UnknownItemsCount,
            session.IsFinalized,
            session.StartedAt,
            session.CompletedAt
        };
    }

    private static bool IsFirstInventory(InventoryAuditFolder folder)
    {
        return folder.Notes?.Contains(FirstInventoryMarker, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    public class CreateRoomRequest
    {
        public string? Name { get; set; }
        public int? SortOrder { get; set; }
    }
}
