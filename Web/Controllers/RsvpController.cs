using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    /// <summary>
    /// Contrôleur pour les pages RSVP publiques (sans authentification)
    /// </summary>
    public class RsvpController : Controller
    {
        private readonly IRsvpService _rsvpService;

        public RsvpController(IRsvpService rsvpService)
        {
            _rsvpService = rsvpService;
        }

        /// <summary>
        /// Page de réponse RSVP accessible via le lien unique
        /// </summary>
        /// <param name="token">Token unique de l'invité</param>
        [HttpGet("Rsvp/{token}")]
        public async Task<IActionResult> Index(string token)
        {
            // Récupérer le token avec les informations de l'invité
            var rsvpToken = await _rsvpService.GetTokenAsync(token);

            if (rsvpToken == null)
            {
                // Token invalide
                return View("InvalidToken");
            }

            // Créer le ViewModel
            var viewModel = new RsvpViewModel
            {
                Token = token,
                Guest = rsvpToken.Guest,
                NumberOfPeople = rsvpToken.Guest.NumberOfPeople,
                DietaryRestrictions = rsvpToken.Guest.DietaryRestrictions,
                IsExpired = rsvpToken.ExpiresAt < DateTime.UtcNow,
                IsAlreadyUsed = rsvpToken.IsUsed,
            };

            // Si le token est déjà utilisé, afficher la page de confirmation
            if (rsvpToken.IsUsed)
            {
                return View("AlreadyResponded", viewModel);
            }

            // Si le token est expiré
            if (viewModel.IsExpired)
            {
                return View("Expired", viewModel);
            }

            // Afficher le formulaire RSVP
            return View(viewModel);
        }

        /// <summary>
        /// Traite la soumission de la réponse RSVP
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(RsvpViewModel viewModel)
        {
            // Valider le token
            if (!await _rsvpService.ValidateTokenAsync(viewModel.Token))
            {
                return View("InvalidToken");
            }

            // Si l'invité refuse, on met NumberOfPeople à 0
            if (viewModel.Status == RsvpStatus.Declined)
            {
                viewModel.NumberOfPeople = 0;
                viewModel.DietaryRestrictions = null;
            }

            // Validation manuelle
            if (viewModel.Status == RsvpStatus.Confirmed)
            {
                if (viewModel.NumberOfPeople < 1 || viewModel.NumberOfPeople > 20)
                {
                    ModelState.AddModelError("NumberOfPeople", "Le nombre de personnes doit être entre 1 et 20");
                }
            }

            if (ModelState.IsValid)
            {
                // Soumettre la réponse RSVP
                var success = await _rsvpService.SubmitRsvpAsync(
                    viewModel.Token,
                    viewModel.Status,
                    viewModel.NumberOfPeople,
                    viewModel.DietaryRestrictions
                );

                if (success)
                {
                    // Récupérer les données mises à jour pour la page de confirmation
                    var rsvpToken = await _rsvpService.GetTokenAsync(viewModel.Token);
                    viewModel.Guest = rsvpToken.Guest;

                    return View("Success", viewModel);
                }
                else
                {
                    TempData["Error"] = "Une erreur est survenue lors de l'enregistrement de votre réponse";
                }
            }

            // Recharger les données de l'invité en cas d'erreur
            var token = await _rsvpService.GetTokenAsync(viewModel.Token);
            viewModel.Guest = token.Guest;

            return View("Index", viewModel);
        }

        /// <summary>
        /// Page d'erreur pour token invalide
        /// </summary>
        public IActionResult InvalidToken()
        {
            return View();
        }

        /// <summary>
        /// Page d'information pour token expiré
        /// </summary>
        public IActionResult Expired()
        {
            return View();
        }
    }
}
