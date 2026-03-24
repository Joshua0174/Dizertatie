using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        // Constructorul care primește opțiunile (ex: ConnectionString-ul) și le trimite la clasa de bază
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Aici definim tabelele noastre specifice
        public DbSet<CitizenProfile> CitizenProfiles { get; set; }
        public DbSet<OfficialProfile> OfficialProfiles { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<CitizenDocument> CitizenDocuments { get; set; }

        public DbSet<DocumentType>DocumentTypes { get; set; }
        public DbSet<CompetencyProfile> CompetencyProfiles { get; set; }
        public DbSet<ProfileDocumentType> ProfileDocumentTypes { get; set; }

        public DbSet<DocumentRequest> DocumentRequests { get; set; }

        public DbSet<Institution> Institutions { get; set; }

        // Aici configurăm regulile speciale ale bazei de date
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // FOARTE IMPORTANT: Trebuie să apelăm linia asta, altfel Identity nu va funcționa!
            base.OnModelCreating(builder);

            // --- Configurare Relație 1-la-1 pentru Cetățean ---
            builder.Entity<AppUser>()
                .HasOne(u => u.CitizenProfile)
                .WithOne(p => p.User)
                .HasForeignKey<CitizenProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Dacă ștergem Userul, se șterge și Profilul

            // --- Configurare Relație 1-la-1 pentru Funcționar ---
            builder.Entity<AppUser>()
                .HasOne(u => u.OfficialProfile)
                .WithOne(p => p.User)
                .HasForeignKey<OfficialProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Configurare GUID automat (NEWID() în SQL) ---
            // Asta face ca SQL Server să genereze singur ID-ul când adaugi o linie nouă
            builder.Entity<CitizenProfile>()
                .Property(p => p.Id)
                .HasDefaultValueSql("NEWID()");

            builder.Entity<OfficialProfile>()
                .Property(p => p.Id)
                .HasDefaultValueSql("NEWID()");

            builder.Entity<ProfileDocumentType>()
            .HasKey(pt => new { pt.CompetencyProfileId, pt.DocumentTypeId });

            builder.Entity<ProfileDocumentType>()
            .HasOne(pt => pt.CompetencyProfile)
            .WithMany(p => p.AllowedDocumentTypes)
            .HasForeignKey(pt => pt.CompetencyProfileId);

            builder.Entity<ProfileDocumentType>()
                .HasOne(pt => pt.DocumentType)
                .WithMany()
                .HasForeignKey(pt => pt.DocumentTypeId);


            builder.Entity<DocumentRequest>()
                    .HasOne(r => r.Official)
                    .WithMany() // Nu avem nevoie de o listă inversă în AppUser momentan
                    .HasForeignKey(r => r.OfficialId)
                    .OnDelete(DeleteBehavior.Restrict); // <--- AICI E FIX-UL (Oprește ștergerea automată)

            builder.Entity<DocumentRequest>()
                .HasOne(r => r.Citizen)
                .WithMany()
                .HasForeignKey(r => r.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AppUser>()
                   .HasOne(u => u.Institution)
                   .WithMany(i => i.Users)
                   .HasForeignKey(u => u.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict); // Nu ștergem instituția dacă ștergem un user, și nici invers automat pentru a preveni erori

            builder.Entity<Institution>()
                .Property(i => i.Id)
                .HasDefaultValueSql("NEWID()");
        }
    }
}
