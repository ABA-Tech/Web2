using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    /// <summary>
    /// Représente une table du plan de salle
    /// </summary>
    public class Table
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la table est requis")]
        [MaxLength(100)]
        [Display(Name = "Nom de la table")]
        public string Name { get; set; }

        [Range(1, 50, ErrorMessage = "La capacité doit être entre 1 et 50")]
        [Display(Name = "Capacité")]
        public int Capacity { get; set; }

        [MaxLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        // Relations
        public ICollection<Guest> Guests { get; set; } = new List<Guest>();

        // Propriétés calculées (non stockées en base)
        /// <summary>
        /// Nombre total de personnes assignées à cette table
        /// </summary>
        [NotMapped]
        [Display(Name = "Occupation actuelle")]
        public int CurrentOccupancy => Guests?.Sum(g => g.NumberOfPeople) ?? 0;

        /// <summary>
        /// Nombre de places restantes
        /// </summary>
        [NotMapped]
        [Display(Name = "Places restantes")]
        public int AvailableSeats => Capacity - CurrentOccupancy;

        /// <summary>
        /// Indique si la table dépasse sa capacité
        /// </summary>
        [NotMapped]
        public bool IsOverCapacity => CurrentOccupancy > Capacity;

        /// <summary>
        /// Pourcentage de remplissage
        /// </summary>
        [NotMapped]
        public double OccupancyPercentage => Capacity > 0 ? (CurrentOccupancy * 100.0 / Capacity) : 0;

        /// <summary>
        /// Nombre d'invités (groupes) à cette table
        /// </summary>
        [NotMapped]
        public int GuestCount => Guests?.Count ?? 0;
    }
}
