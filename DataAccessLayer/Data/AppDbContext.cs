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
        }
    }
}
