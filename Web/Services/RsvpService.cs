using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services
{
    /// <summary>
    /// Service de gestion des réponses RSVP et des tokens
    /// </summary>
    public class RsvpService : IRsvpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<RsvpService> _logger;

        public RsvpService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<RsvpService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Génère un nouveau token RSVP pour un invité
        /// </summary>
        public async Task<RsvpToken> GenerateTokenAsync(int guestId, int expirationDays)
        {
            try
            {
                // Vérifier si l'invité existe
                var guest = await _context.Guests.FindAsync(guestId);
                if (guest == null)
                {
                    _logger.LogWarning($"Impossible de générer un token : invité {guestId} introuvable");
                    return null;
                }

                // Supprimer l'ancien token s'il existe
                var existingToken = await _context.RsvpTokens
                    .FirstOrDefaultAsync(t => t.GuestId == guestId);

                if (existingToken != null)
                {
                    _context.RsvpTokens.Remove(existingToken);
                }

                // Créer un nouveau token
                var token = new RsvpToken
                {
                    Token = Guid.NewGuid().ToString("N"), // Token sans tirets
                    GuestId = guestId,
                    ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                _context.RsvpTokens.Add(token);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Token RSVP généré pour l'invité {guestId}");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la génération du token pour l'invité {guestId}");
                return null;
            }
        }

        /// <summary>
        /// Récupère un token RSVP avec les informations de l'invité
        /// </summary>
        public async Task<RsvpToken> GetTokenAsync(string token)
        {
            try
            {
                return await _context.RsvpTokens
                    .Include(t => t.Guest)
                    .FirstOrDefaultAsync(t => t.Token == token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération du token {token}");
                return null;
            }
        }

        /// <summary>
        /// Valide qu'un token est utilisable (existe, non expiré, non utilisé)
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var rsvpToken = await GetTokenAsync(token);

                if (rsvpToken == null)
                {
                    _logger.LogWarning($"Token invalide : {token}");
                    return false;
                }

                if (rsvpToken.IsUsed)
                {
                    _logger.LogWarning($"Token déjà utilisé : {token}");
                    return false;
                }

                if (DateTime.UtcNow > rsvpToken.ExpiresAt)
                {
                    _logger.LogWarning($"Token expiré : {token}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la validation du token {token}");
                return false;
            }
        }

        /// <summary>
        /// Soumet une réponse RSVP
        /// </summary>
        public async Task<bool> SubmitRsvpAsync(string token, RsvpStatus status, int numberOfPeople, string dietaryRestrictions)
        {
            try
            {
                // Valider le token
                if (!await ValidateTokenAsync(token))
                {
                    return false;
                }

                // Récupérer le token et l'invité
                var rsvpToken = await GetTokenAsync(token);
                var guest = rsvpToken.Guest;

                // Mettre à jour l'invité
                guest.Status = status;
                guest.NumberOfPeople = status == RsvpStatus.Confirmed ? numberOfPeople : 0;
                guest.DietaryRestrictions = dietaryRestrictions;
                guest.RespondedAt = DateTime.UtcNow;

                // Marquer le token comme utilisé
                rsvpToken.IsUsed = true;
                rsvpToken.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Envoyer l'email de confirmation
                if (status == RsvpStatus.Confirmed)
                {
                    await _emailService.SendConfirmationEmailAsync(guest);
                }
                else if (status == RsvpStatus.Declined)
                {
                    await _emailService.SendDeclineConfirmationAsync(guest);
                }

                _logger.LogInformation($"RSVP soumis avec succès pour l'invité {guest.FullName} (Statut: {status})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la soumission du RSVP avec le token {token}");
                return false;
            }
        }

        /// <summary>
        /// Génère l'URL complète du RSVP
        /// </summary>
        public async Task<string> GetRsvpUrlAsync(string token)
        {
            var baseUrl = _configuration["RsvpSettings:BaseUrl"];
            return $"{baseUrl}/Rsvp/{token}";
        }

        /// <summary>
        /// Régénère un token pour un invité (utile si le premier est expiré ou perdu)
        /// </summary>
        public async Task<bool> RegenerateTokenAsync(int guestId)
        {
            try
            {
                var expirationDays = int.Parse(_configuration["RsvpSettings:TokenExpirationDays"]);
                var token = await GenerateTokenAsync(guestId, expirationDays);

                if (token == null)
                {
                    return false;
                }

                // Récupérer l'invité et envoyer le nouvel email
                var guest = await _context.Guests.FindAsync(guestId);
                if (guest != null)
                {
                    var rsvpUrl = await GetRsvpUrlAsync(token.Token);
                    await _emailService.SendRsvpInvitationAsync(guest, rsvpUrl);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la régénération du token pour l'invité {guestId}");
                return false;
            }
        }
    }
}
