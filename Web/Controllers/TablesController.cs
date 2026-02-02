using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Data;
using Web.Models.ViewModels;
using Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion du plan de table
    /// </summary>
   // [Authorize]
    public class TablesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TablesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Vue du plan de table complet
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var viewModel = new SeatingPlanViewModel
            {
                Tables = await _context.Tables
                    .Include(t => t.Guests)
                    .OrderBy(t => t.Name)
                    .ToListAsync(),

                UnassignedGuests = await _context.Guests
                    .Where(g => g.TableId == null && g.Status == RsvpStatus.Confirmed)
                    .OrderBy(g => g.LastName)
                    .ThenBy(g => g.FirstName)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Affiche le formulaire de création d'une table
        /// </summary>
        public IActionResult Create()
        {
            return View(new Table());
        }

        /// <summary>
        /// Crée une nouvelle table
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Table table)
        {
            if (ModelState.IsValid)
            {
                // Vérifier l'unicité du nom
                var exists = await _context.Tables.AnyAsync(t => t.Name == table.Name);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Une table avec ce nom existe déjà");
                    return View(table);
                }

                _context.Tables.Add(table);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Table '{table.Name}' créée avec succès";
                return RedirectToAction(nameof(Index));
            }

            return View(table);
        }

        /// <summary>
        /// Affiche le formulaire d'édition d'une table
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.Tables
                .Include(t => t.Guests)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            return View(table);
        }

        /// <summary>
        /// Modifie une table existante
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Table table)
        {
            if (id != table.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Vérifier l'unicité du nom
                var exists = await _context.Tables.AnyAsync(t => t.Name == table.Name && t.Id != id);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Une table avec ce nom existe déjà");
                    return View(table);
                }

                var existingTable = await _context.Tables.FindAsync(id);
                if (existingTable == null)
                {
                    return NotFound();
                }

                existingTable.Name = table.Name;
                existingTable.Capacity = table.Capacity;
                existingTable.Description = table.Description;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Table modifiée avec succès";
                return RedirectToAction(nameof(Index));
            }

            return View(table);
        }

        /// <summary>
        /// Supprime une table (les invités sont désassignés, pas supprimés)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.Tables
                .Include(t => t.Guests)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            // Désassigner tous les invités de cette table
            foreach (var guest in table.Guests)
            {
                guest.TableId = null;
            }

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Table '{table.Name}' supprimée (invités désassignés)";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Assigne un invité à une table
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignGuest(int guestId, int tableId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            var table = await _context.Tables
                .Include(t => t.Guests)
                .FirstOrDefaultAsync(t => t.Id == tableId);

            if (guest == null || table == null)
            {
                return Json(new { success = false, message = "Invité ou table introuvable" });
            }

            // Vérifier si l'ajout dépasserait la capacité
            var futureOccupancy = table.CurrentOccupancy + guest.NumberOfPeople;
            if (futureOccupancy > table.Capacity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Capacité dépassée : {futureOccupancy}/{table.Capacity} places",
                    warning = true
                });
            }

            guest.TableId = tableId;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{guest.FullName} assigné(e) à {table.Name}",
                tableOccupancy = table.CurrentOccupancy,
                tableCapacity = table.Capacity
            });
        }

        /// <summary>
        /// Retire un invité d'une table
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UnassignGuest(int guestId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null)
            {
                return Json(new { success = false, message = "Invité introuvable" });
            }

            guest.TableId = null;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{guest.FullName} retiré(e) de la table"
            });
        }

        /// <summary>
        /// Détails d'une table avec tous ses invités
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var table = await _context.Tables
                .Include(t => t.Guests)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
            {
                return NotFound();
            }

            return View(table);
        }

        /// <summary>
        /// API JSON pour récupérer les données du plan de table (pour drag & drop)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSeatingData()
        {
            var tables = await _context.Tables
                .Include(t => t.Guests)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Capacity,
                    CurrentOccupancy = t.CurrentOccupancy,
                    AvailableSeats = t.AvailableSeats,
                    IsOverCapacity = t.IsOverCapacity,
                    Guests = t.Guests.Select(g => new
                    {
                        g.Id,
                        g.FullName,
                        g.NumberOfPeople,
                        g.GroupFamily
                    }).ToList()
                })
                .ToListAsync();

            var unassignedGuests = await _context.Guests
                .Where(g => g.TableId == null && g.Status == RsvpStatus.Confirmed)
                .Select(g => new
                {
                    g.Id,
                    g.FullName,
                    g.NumberOfPeople,
                    g.GroupFamily
                })
                .ToListAsync();

            return Json(new { tables, unassignedGuests });
        }
    }
}
