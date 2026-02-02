using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    /// <summary>
    /// Représente un invité au mariage
    /// </summary>
    public class Guest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [MaxLength(100)]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(100)]
        [Display(Name = "Nom")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [MaxLength(200)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MaxLength(100)]
        [Display(Name = "Groupe/Famille")]
        public string GroupFamily { get; set; }

        [Range(1, 20, ErrorMessage = "Le nombre de personnes doit être entre 1 et 20")]
        [Display(Name = "Nombre de personnes")]
        public int NumberOfPeople { get; set; } = 1;

        [Display(Name = "Statut")]
        public RsvpStatus Status { get; set; } = RsvpStatus.Pending;

        [MaxLength(500)]
        [Display(Name = "Contraintes alimentaires")]
        public string DietaryRestrictions { get; set; }

        // Relations
        [Display(Name = "Table")]
        public int? TableId { get; set; }
        public Table Table { get; set; }

        public RsvpToken RsvpToken { get; set; }

        // Metadata
        [Display(Name = "Créé le")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modifié le")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Répondu le")]
        public DateTime? RespondedAt { get; set; }

        // Propriétés calculées
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            RsvpStatus.Pending => "En attente",
            RsvpStatus.Confirmed => "Confirmé",
            RsvpStatus.Declined => "Refusé",
            _ => "Inconnu"
        };
    }

    /// <summary>
    /// Statuts possibles pour la réponse RSVP
    /// </summary>
    public enum RsvpStatus
    {
        [Display(Name = "En attente")]
        Pending,

        [Display(Name = "Confirmé")]
        Confirmed,

        [Display(Name = "Refusé")]
        Declined
    }
}
