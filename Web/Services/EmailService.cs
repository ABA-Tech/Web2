using System.Net.Mail;
using System.Net;
using Web.Models;
using Microsoft.Extensions.Options;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


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
                //var body = GenerateInvitationEmailBody(guest, rsvpUrl);
                var body = SendInvitation(guest, rsvpUrl);

               // return await SendEmailAsync(guest.Email, subject, body);
                return await SendEmailAsync(guest.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de l'invitation à {guest.Email}");
                return false;
            }
        }
        public string SendInvitation(Guest guest, string baseUrl)
        {
            var invitationLink = baseUrl;

            var emailBody = $@"
                <h1>Vous êtes invité(e) au mariage de Sophie & Alexandre !</h1>
                <p>Cher(e) {guest.FirstName} {guest.LastName},</p>
                <p>Cliquez sur le lien ci-dessous pour consulter votre invitation :</p>
                <a href='{invitationLink}' style='display: inline-block; padding: 15px 30px; background: #8bc34a; color: white; text-decoration: none; border-radius: 5px;'>
                    Voir mon invitation
                </a>
            ";

            return emailBody;
        }
        /// <summary>
        /// Envoie l'email de confirmation de présence
        /// </summary>
        public async Task<bool> SendConfirmationEmailAsync(Guest guest, string invitationUrl)
        {
            try
            {
                var subject = "✅ Confirmation de votre présence à notre mariage";

                var body = GenerateConfirmationEmailBody(guest, invitationUrl);

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
            return await SendEmailWithResend(to, subject, htmlBody);
        }
        
        private async Task<bool> SendEmailAsyncWithGmail(string to, string subject, string htmlBody)
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
        private async Task<bool> SendEmailAsyncWithBrevo(string toEmail, string subject, string body)
        {
            try
            {
                // Récupération de la clé depuis la variable d'environnement
                var apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine("Error: BREVO_API_KEY is missing!");
                    return false;
                }
                Console.WriteLine($"BREVO_API_KEY présente : {apiKey}");
                Console.WriteLine($"BREVO_API_KEY présente : {!string.IsNullOrEmpty(apiKey)}");
                Console.WriteLine($"Longueur de la clé : {apiKey?.Length ?? 0}");
                using var httpClient = new HttpClient();
        
                // Authentification avec Bearer token
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );
        
                // Création du payload JSON
                var payload = new
                {
                    sender = new
                    {
                        email = "magaliperlin237@gmail.com",
                        name = "Laura"
                    },
                    to = new[]
                    {
                        new { email = toEmail }
                    },
                    subject = subject,
                    htmlContent = body
                };
        
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
        
                // Envoi de la requête
                var response = await httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
        
                // Log et vérification du statut HTTP
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Brevo API error: HTTP {response.StatusCode}");
                    Console.WriteLine($"Response: {responseBody}");
                    return false;
                }
        
                Console.WriteLine($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when sending email: {ex}");
                return false;
            }
        }

        private async Task<bool> SendEmailWithResend(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                
                var payload = new
                {
                    from = "admin@blandine-mafeu.fr", // Utilisez ce domaine pour tester
                    to = new[] { toEmail },
                    subject = subject,
                    html = htmlBody
                };
                Console.WriteLine(apiKey);
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("https://api.resend.com/emails", content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Resend error: {responseBody}");
                    return false;
                }
                
                _logger.LogInformation($"Email envoyé via Resend à {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _logger.LogError(ex, "Erreur Resend");
                return false;
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
        private string GenerateConfirmationEmailBody(Guest guest, string invitationUrl)
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
        .button {{ display: inline-block; background: #10b981; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .button:hover {{ background: #059669; }}
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
            
            <p style='text-align: center;'>
                <a href='{invitationUrl}' class='button'>📋 Voir mon invitation</a>
            </p>
            
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
