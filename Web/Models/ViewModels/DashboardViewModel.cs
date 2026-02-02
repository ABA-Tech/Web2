using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels
{
    /// <summary>
    /// ViewModel pour le tableau de bord administrateur
    /// </summary>
    public class DashboardViewModel
    {
        // Statistiques globales
        public int TotalGuests { get; set; }
        public int TotalPeople { get; set; }
        public int ConfirmedGuests { get; set; }
        public int ConfirmedPeople { get; set; }
        public int DeclinedGuests { get; set; }
        public int DeclinedPeople { get; set; }
        public int PendingGuests { get; set; }
        public int PendingPeople { get; set; }

        // Statistiques tables
        public int TotalTables { get; set; }
        public int TotalSeats { get; set; }
        public int OccupiedSeats { get; set; }
        public int AvailableSeats { get; set; }

        // Pourcentages
        public double ConfirmationRate => TotalGuests > 0 ? (ConfirmedGuests * 100.0 / TotalGuests) : 0;
        public double SeatingOccupancy => TotalSeats > 0 ? (OccupiedSeats * 100.0 / TotalSeats) : 0;

        // Données détaillées
        public List<Guest> RecentResponses { get; set; } = new List<Guest>();
        public List<Guest> PendingGuestsList { get; set; } = new List<Guest>();
        public List<Table> OverCapacityTables { get; set; } = new List<Table>();
    }

    /// <summary>
    /// ViewModel pour la page RSVP publique
    /// </summary>
    public class RsvpViewModel
    {
        public string Token { get; set; }
        public Guest? Guest { get; set; }

        [Required(ErrorMessage = "Veuillez indiquer votre présence")]
        [Display(Name = "Serez-vous présent(e) ?")]
        public RsvpStatus Status { get; set; }

        [Range(1, 20, ErrorMessage = "Le nombre de personnes doit être entre 1 et 20")]
        [Display(Name = "Nombre de personnes")]
        public int NumberOfPeople { get; set; }

        [MaxLength(500)]
        [Display(Name = "Contraintes alimentaires ou allergies")]
        public string DietaryRestrictions { get; set; }

        public bool IsExpired { get; set; }
        public bool IsAlreadyUsed { get; set; }
    }

    /// <summary>
    /// ViewModel pour la création/édition d'un invité
    /// </summary>
    public class GuestFormViewModel
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

        [Display(Name = "Table assignée")]
        public int? TableId { get; set; }

        [Display(Name = "Envoyer l'invitation par email")]
        public bool SendInvitation { get; set; } = true;
    }

    /// <summary>
    /// ViewModel pour l'assignation de table
    /// </summary>
    public class AssignTableViewModel
    {
        [Required]
        public int GuestId { get; set; }

        [Required]
        public int TableId { get; set; }

        public Guest Guest { get; set; }
        public List<Table> AvailableTables { get; set; } = new List<Table>();
    }

    /// <summary>
    /// ViewModel pour la liste des invités avec filtres
    /// </summary>
    public class GuestListViewModel
    {
        public List<Guest> Guests { get; set; } = new List<Guest>();

        // Filtres
        public string SearchTerm { get; set; }
        public RsvpStatus? StatusFilter { get; set; }
        public int? TableFilter { get; set; }
        public string GroupFilter { get; set; }

        // Options disponibles pour les filtres
        public List<Table> Tables { get; set; } = new List<Table>();
        public List<string> Groups { get; set; } = new List<string>();

        // Statistiques de la liste filtrée
        public int TotalGuests => Guests.Count;
        public int TotalPeople => Guests.Sum(g => g.NumberOfPeople);
    }

    /// <summary>
    /// ViewModel pour le plan de table
    /// </summary>
    public class SeatingPlanViewModel
    {
        public List<Table> Tables { get; set; } = new List<Table>();
        public List<Guest> UnassignedGuests { get; set; } = new List<Guest>();

        public int TotalSeats => Tables.Sum(t => t.Capacity);
        public int OccupiedSeats => Tables.Sum(t => t.CurrentOccupancy);
        public int AvailableSeats => TotalSeats - OccupiedSeats;
        public int UnassignedPeople => UnassignedGuests.Sum(g => g.NumberOfPeople);
    }
}
