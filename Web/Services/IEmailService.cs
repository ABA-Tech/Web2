using Web.Models;

namespace Web.Services
{
    /// <summary>
    /// Interface pour le service d'envoi d'emails
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendRsvpInvitationAsync(Guest guest, string rsvpUrl);
        Task<bool> SendConfirmationEmailAsync(Guest guest);
        Task<bool> SendDeclineConfirmationAsync(Guest guest);
    }
}
