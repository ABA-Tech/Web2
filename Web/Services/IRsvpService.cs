using Web.Models;

namespace Web.Services
{
    /// <summary>
    /// Interface pour le service de gestion des RSVP
    /// </summary>
    public interface IRsvpService
    {
        Task<RsvpToken> GenerateTokenAsync(int guestId, int expirationDays);
        Task<RsvpToken> GetTokenAsync(string token);
        Task<bool> ValidateTokenAsync(string token);
        Task<bool> SubmitRsvpAsync(string token, RsvpStatus status, int numberOfPeople, string dietaryRestrictions);
        Task<string> GetRsvpUrlAsync(string token);
        Task<bool> RegenerateTokenAsync(int guestId);
    }
}
