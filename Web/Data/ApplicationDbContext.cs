using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

        // DbSets pour chaque entité
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<RsvpToken> RsvpTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration de l'entité Guest
            modelBuilder.Entity<Guest>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Index pour améliorer les performances des recherches
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.GroupFamily);

                // Relation avec Table (un invité peut être à une table, une table peut avoir plusieurs invités)
                entity.HasOne(e => e.Table)
                    .WithMany(t => t.Guests)
                    .HasForeignKey(e => e.TableId)
                    .OnDelete(DeleteBehavior.SetNull); // Si on supprime une table, on ne supprime pas les invités

                // Relation avec RsvpToken (un invité a un token, un token appartient à un invité)
                entity.HasOne(e => e.RsvpToken)
                    .WithOne(t => t.Guest)
                    .HasForeignKey<RsvpToken>(t => t.GuestId)
                    .OnDelete(DeleteBehavior.Cascade); // Si on supprime un invité, on supprime son token
            });

            // Configuration de l'entité Table
            modelBuilder.Entity<Table>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Contrainte d'unicité sur le nom de la table
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configuration de l'entité RsvpToken
            modelBuilder.Entity<RsvpToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Index unique sur le token pour garantir l'unicité
                entity.HasIndex(e => e.Token).IsUnique();

                // Index sur la date d'expiration pour les requêtes de nettoyage
                entity.HasIndex(e => e.ExpiresAt);

                // Index sur IsUsed pour les requêtes de validation
                entity.HasIndex(e => e.IsUsed);
            });

            // Données de seed (optionnel - pour les tests)
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Méthode pour insérer des données initiales (optionnel)
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Exemple: créer quelques tables par défaut
            modelBuilder.Entity<Table>().HasData(
                new Table { Id = 1, Name = "Table 1", Capacity = 8, Description = "Table près de la piste de danse" },
                new Table { Id = 2, Name = "Table 2", Capacity = 8, Description = "Table côté jardin" },
                new Table { Id = 3, Name = "Table 3", Capacity = 10, Description = "Grande table familiale" }
            );
        }

        /// <summary>
        /// Override de SaveChanges pour gérer automatiquement les dates de mise à jour
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override de SaveChangesAsync pour gérer automatiquement les dates de mise à jour
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Met à jour automatiquement les timestamps lors de la sauvegarde
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Guest && (e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                if (entry.Entity is Guest guest)
                {
                    guest.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
}

