using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    /// <summary>
    /// Jeton unique pour l'accès RSVP d'un invité
    /// </summary>
    public class RsvpToken
    {
        public int Id { get; set; }

        /// <summary>
        /// Token unique (GUID) pour identifier l'invité
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Token { get; set; }

        /// <summary>
        /// ID de l'invité associé
        /// </summary>
        public int GuestId { get; set; }

        /// <summary>
        /// Invité associé
        /// </summary>
        public Guest Guest { get; set; }

        /// <summary>
        /// Date d'expiration du token
        /// </summary>
        [Display(Name = "Expire le")]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Indique si le token a été utilisé
        /// </summary>
        [Display(Name = "Utilisé")]
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Date d'utilisation du token
        /// </summary>
        [Display(Name = "Utilisé le")]
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Date de création du token
        /// </summary>
        [Display(Name = "Créé le")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Vérifie si le token est valide (non utilisé et non expiré)
        /// </summary>
        [NotMapped]
        public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// Nombre de jours restants avant expiration
        /// </summary>
        [NotMapped]
        public int DaysUntilExpiration => IsValid ? (ExpiresAt - DateTime.UtcNow).Days : 0;
    }
    }
