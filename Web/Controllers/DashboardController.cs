using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Data;
using Web.Models.ViewModels;
using Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    /// <summary>
    /// Contrôleur pour le tableau de bord administrateur
    /// </summary>
   // [Authorize] // Nécessite l'authentification
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Page principale du tableau de bord
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            // Récupérer tous les invités
            var guests = await _context.Guests
                .Include(g => g.Table)
                .ToListAsync();

            // Statistiques globales des invités
            viewModel.TotalGuests = guests.Count;
            viewModel.TotalPeople = guests.Sum(g => g.NumberOfPeople);

            var confirmedGuests = guests.Where(g => g.Status == RsvpStatus.Confirmed).ToList();
            viewModel.ConfirmedGuests = confirmedGuests.Count;
            viewModel.ConfirmedPeople = confirmedGuests.Sum(g => g.NumberOfPeople);

            var declinedGuests = guests.Where(g => g.Status == RsvpStatus.Declined).ToList();
            viewModel.DeclinedGuests = declinedGuests.Count;
            viewModel.DeclinedPeople = declinedGuests.Sum(g => g.NumberOfPeople);

            var pendingGuests = guests.Where(g => g.Status == RsvpStatus.Pending).ToList();
            viewModel.PendingGuests = pendingGuests.Count;
            viewModel.PendingPeople = pendingGuests.Sum(g => g.NumberOfPeople);

            // Statistiques des tables
            var tables = await _context.Tables
                .Include(t => t.Guests)
                .ToListAsync();

            viewModel.TotalTables = tables.Count;
            viewModel.TotalSeats = tables.Sum(t => t.Capacity);
            viewModel.OccupiedSeats = tables.Sum(t => t.CurrentOccupancy);
            viewModel.AvailableSeats = viewModel.TotalSeats - viewModel.OccupiedSeats;

            // Réponses récentes (10 dernières)
            viewModel.RecentResponses = guests
                .Where(g => g.RespondedAt.HasValue)
                .OrderByDescending(g => g.RespondedAt)
                .Take(10)
                .ToList();

            // Invités en attente
            viewModel.PendingGuestsList = pendingGuests
                .OrderBy(g => g.LastName)
                .ThenBy(g => g.FirstName)
                .Take(10)
                .ToList();

            // Tables dépassant la capacité
            viewModel.OverCapacityTables = tables
                .Where(t => t.IsOverCapacity)
                .OrderByDescending(t => t.CurrentOccupancy - t.Capacity)
                .ToList();

            return View(viewModel);
        }

        /// <summary>
        /// Exporte les statistiques en JSON (pour API ou graphiques)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var guests = await _context.Guests.ToListAsync();
            var tables = await _context.Tables.Include(t => t.Guests).ToListAsync();

            var stats = new
            {
                Guests = new
                {
                    Total = guests.Count,
                    TotalPeople = guests.Sum(g => g.NumberOfPeople),
                    Confirmed = guests.Count(g => g.Status == RsvpStatus.Confirmed),
                    ConfirmedPeople = guests.Where(g => g.Status == RsvpStatus.Confirmed).Sum(g => g.NumberOfPeople),
                    Declined = guests.Count(g => g.Status == RsvpStatus.Declined),
                    Pending = guests.Count(g => g.Status == RsvpStatus.Pending),
                    ConfirmationRate = guests.Count > 0
                        ? Math.Round(guests.Count(g => g.Status == RsvpStatus.Confirmed) * 100.0 / guests.Count, 1)
                        : 0
                },
                Tables = new
                {
                    Total = tables.Count,
                    TotalCapacity = tables.Sum(t => t.Capacity),
                    Occupied = tables.Sum(t => t.CurrentOccupancy),
                    Available = tables.Sum(t => t.AvailableSeats),
                    OverCapacity = tables.Count(t => t.IsOverCapacity)
                },
                ByGroup = guests
                    .Where(g => !string.IsNullOrEmpty(g.GroupFamily))
                    .GroupBy(g => g.GroupFamily)
                    .Select(g => new
                    {
                        Group = g.Key,
                        Count = g.Count(),
                        TotalPeople = g.Sum(x => x.NumberOfPeople),
                        Confirmed = g.Count(x => x.Status == RsvpStatus.Confirmed)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList()
            };

            return Json(stats);
        }
    }
}
