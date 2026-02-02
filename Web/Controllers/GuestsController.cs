using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Web.Data;
using Web.Models.ViewModels;
using Web.Models;
using Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des invités (CRUD complet)
    /// </summary>
   // [Authorize]
    public class GuestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRsvpService _rsvpService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public GuestsController(
            ApplicationDbContext context,
            IRsvpService rsvpService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _rsvpService = rsvpService;
            _emailService = emailService;
            _configuration = configuration;
        }

        /// <summary>
        /// Liste des invités avec filtres
        /// </summary>
        public async Task<IActionResult> Index(string searchTerm, RsvpStatus? statusFilter, int? tableFilter, string groupFilter)
        {
            var viewModel = new GuestListViewModel
            {
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                TableFilter = tableFilter,
                GroupFilter = groupFilter
            };

            // Query de base
            var query = _context.Guests
                .Include(g => g.Table)
                .AsQueryable();

            // Appliquer les filtres
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(g =>
                    g.FirstName.Contains(searchTerm) ||
                    g.LastName.Contains(searchTerm) ||
                    g.Email.Contains(searchTerm));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(g => g.Status == statusFilter.Value);
            }

            if (tableFilter.HasValue)
            {
                query = query.Where(g => g.TableId == tableFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(groupFilter))
            {
                query = query.Where(g => g.GroupFamily == groupFilter);
            }

            viewModel.Guests = await query
                .OrderBy(g => g.LastName)
                .ThenBy(g => g.FirstName)
                .ToListAsync();

            // Charger les options pour les filtres
            viewModel.Tables = await _context.Tables.OrderBy(t => t.Name).ToListAsync();
            viewModel.Groups = await _context.Guests
                .Where(g => !string.IsNullOrEmpty(g.GroupFamily))
                .Select(g => g.GroupFamily)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return View(viewModel);
        }

        /// <summary>
        /// Affiche le formulaire de création d'un invité
        /// </summary>
        public async Task<IActionResult> Create()
        {
            var viewModel = new GuestFormViewModel();
            await LoadTablesForForm();
            return View(viewModel);
        }

        /// <summary>
        /// Traite la création d'un nouvel invité
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GuestFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Créer le nouvel invité
                var guest = new Guest
                {
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    Email = viewModel.Email,
                    GroupFamily = viewModel.GroupFamily,
                    NumberOfPeople = viewModel.NumberOfPeople,
                    Status = viewModel.Status,
                    DietaryRestrictions = viewModel.DietaryRestrictions,
                    TableId = viewModel.TableId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();

                // Générer le token RSVP et envoyer l'email si demandé
                if (viewModel.SendInvitation)
                {
                    var expirationDays = int.Parse(_configuration["RsvpSettings:TokenExpirationDays"]);
                    var token = await _rsvpService.GenerateTokenAsync(guest.Id, expirationDays);

                    if (token != null)
                    {
                        var rsvpUrl = await _rsvpService.GetRsvpUrlAsync(token.Token);
                        await _emailService.SendRsvpInvitationAsync(guest, rsvpUrl);
                        TempData["Success"] = $"Invité créé et invitation envoyée à {guest.Email}";
                    }
                    else
                    {
                        TempData["Warning"] = "Invité créé mais erreur lors de l'envoi de l'invitation";
                    }
                }
                else
                {
                    TempData["Success"] = "Invité créé avec succès";
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadTablesForForm();
            return View(viewModel);
        }

        /// <summary>
        /// Affiche le formulaire d'édition d'un invité
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null)
            {
                return NotFound();
            }

            var viewModel = new GuestFormViewModel
            {
                Id = guest.Id,
                FirstName = guest.FirstName,
                LastName = guest.LastName,
                Email = guest.Email,
                GroupFamily = guest.GroupFamily,
                NumberOfPeople = guest.NumberOfPeople,
                Status = guest.Status,
                DietaryRestrictions = guest.DietaryRestrictions,
                TableId = guest.TableId,
                SendInvitation = false
            };

            await LoadTablesForForm();
            return View(viewModel);
        }

        /// <summary>
        /// Traite la modification d'un invité
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GuestFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var guest = await _context.Guests.FindAsync(id);
                if (guest == null)
                {
                    return NotFound();
                }

                guest.FirstName = viewModel.FirstName;
                guest.LastName = viewModel.LastName;
                guest.Email = viewModel.Email;
                guest.GroupFamily = viewModel.GroupFamily;
                guest.NumberOfPeople = viewModel.NumberOfPeople;
                guest.Status = viewModel.Status;
                guest.DietaryRestrictions = viewModel.DietaryRestrictions;
                guest.TableId = viewModel.TableId;
                guest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Invité modifié avec succès";
                return RedirectToAction(nameof(Index));
            }

            await LoadTablesForForm();
            return View(viewModel);
        }

        /// <summary>
        /// Affiche la confirmation de suppression
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var guest = await _context.Guests
                .Include(g => g.Table)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guest == null)
            {
                return NotFound();
            }

            return View(guest);
        }

        /// <summary>
        /// Supprime un invité
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null)
            {
                return NotFound();
            }

            _context.Guests.Remove(guest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Invité supprimé avec succès";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Renvoie l'invitation RSVP par email
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResendInvitation(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null)
            {
                return NotFound();
            }

            // Régénérer le token et envoyer l'email
            var success = await _rsvpService.RegenerateTokenAsync(id);

            if (success)
            {
                TempData["Success"] = $"Invitation renvoyée à {guest.Email}";
            }
            else
            {
                TempData["Error"] = "Erreur lors de l'envoi de l'invitation";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Détails d'un invité
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var guest = await _context.Guests
                .Include(g => g.Table)
                .Include(g => g.RsvpToken)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guest == null)
            {
                return NotFound();
            }

            return View(guest);
        }

        /// <summary>
        /// Charge les tables disponibles pour le formulaire
        /// </summary>
        private async Task LoadTablesForForm()
        {
            var tables = await _context.Tables.OrderBy(t => t.Name).ToListAsync();
            ViewBag.Tables = new SelectList(tables, "Id", "Name");
        }
    }
}
