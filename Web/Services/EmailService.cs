using System.Net.Mail;
using System.Net;
using Web.Models;
using Microsoft.Extensions.Options;

namespace Web.Services
{
    /// <summary>
    /// Service d'envoi d'emails pour les invitations et confirmations
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// Envoie l'invitation RSVP par email
        /// </summary>
        public async Task<bool> SendRsvpInvitationAsync(Guest guest, string rsvpUrl)
        {
            try
            {
                var subject = "🎉 Vous êtes invité(e) à notre mariage !";
                var body = GenerateInvitationEmailBody(guest, rsvpUrl);

               // return await SendEmailAsync(guest.Email, subject, body);
                return await SendEmailAsync(guest.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de l'invitation à {guest.Email}");
                return false;
            }
        }

        /// <summary>
        /// Envoie l'email de confirmation de présence
        /// </summary>
        public async Task<bool> SendConfirmationEmailAsync(Guest guest)
        {
            try
            {
                var subject = "✅ Confirmation de votre présence à notre mariage";
                var body = GenerateConfirmationEmailBody(guest);

                return await SendEmailAsync(guest.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de la confirmation à {guest.Email}");
                return false;
            }
        }

        /// <summary>
        /// Envoie l'email de confirmation d'absence
        /// </summary>
        public async Task<bool> SendDeclineConfirmationAsync(Guest guest)
        {
            try
            {
                var subject = "Accusé de réception - Mariage";
                var body = GenerateDeclineEmailBody(guest);

                return await SendEmailAsync(guest.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de la confirmation d'absence à {guest.Email}");
                return false;
            }
        }

        /// <summary>
        /// Méthode générique d'envoi d'email
        /// </summary>

        private async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email envoyé à {Email}: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email à {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Génère le corps HTML de l'email d'invitation
        /// </summary>
        private string GenerateInvitationEmailBody(Guest guest, string rsvpUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Vous êtes invité(e) !</h1>
        </div>
        <div class='content'>
            <p>Bonjour {guest.FullName},</p>
            
            <p>Nous avons le plaisir de vous inviter à célébrer notre mariage !</p>
            
            <p><strong>Merci de confirmer votre présence en cliquant sur le bouton ci-dessous :</strong></p>
            
            <div style='text-align: center;'>
                <a href='{rsvpUrl}' class='button'>Confirmer ma présence</a>
            </div>
            
            <p>Vous pourrez également nous indiquer :</p>
            <ul>
                <li>Le nombre de personnes qui vous accompagnent</li>
                <li>Vos éventuelles contraintes alimentaires</li>
            </ul>
            
            <p><em>Ce lien est personnel et unique. Merci de ne pas le partager.</em></p>
            
            <p>Nous avons hâte de partager ce moment spécial avec vous !</p>
            
            <p>À très bientôt,<br>Les mariés 💑</p>
        </div>
        <div class='footer'>
            <p>Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :<br>
            <a href='{rsvpUrl}'>{rsvpUrl}</a></p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Génère le corps HTML de l'email de confirmation
        /// </summary>
        private string GenerateConfirmationEmailBody(Guest guest)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #10b981; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info-box {{ background: white; padding: 15px; border-left: 4px solid #10b981; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Confirmation reçue !</h1>
        </div>
        <div class='content'>
            <p>Bonjour {guest.FullName},</p>
            
            <p>Merci d'avoir confirmé votre présence à notre mariage ! 🎊</p>
            
            <div class='info-box'>
                <p><strong>Récapitulatif de votre réponse :</strong></p>
                <p>Nombre de personnes : <strong>{guest.NumberOfPeople}</strong></p>
                {(string.IsNullOrWhiteSpace(guest.DietaryRestrictions) ? "" : $"<p>Contraintes alimentaires : <strong>{guest.DietaryRestrictions}</strong></p>")}
            </div>
            
            <p>Nous sommes ravis de pouvoir partager ce moment avec vous !</p>
            
            <p>D'autres informations pratiques vous seront communiquées prochainement.</p>
            
            <p>À très bientôt,<br>Les mariés 💑</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Génère le corps HTML de l'email de confirmation d'absence
        /// </summary>
        private string GenerateDeclineEmailBody(Guest guest)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #6b7280; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Réponse reçue</h1>
        </div>
        <div class='content'>
            <p>Bonjour {guest.FullName},</p>
            
            <p>Nous avons bien reçu votre réponse.</p>
            
            <p>Nous sommes désolés que vous ne puissiez pas être présent(e) à notre mariage.</p>
            
            <p>Nous pensons à vous et espérons pouvoir célébrer avec vous une prochaine fois.</p>
            
            <p>Bien à vous,<br>Les mariés</p>
        </div>
    </div>
</body>
</html>";
        }
    }


    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}
