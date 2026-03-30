using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // =========================================================================
            // 0. SEED PENTRU CATEGORII DE DOCUMENTE
            // =========================================================================
            if (!await context.DocumentCategories.AnyAsync())
            {
                Console.WriteLine(">>> Seeding Categorii Documente...");
                var categorii = new List<DocumentCategory>
                {
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555551"), Name = "Identitate" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555552"), Name = "Auto & Transport" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555553"), Name = "Sănătate & Medical" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555554"), Name = "Educație" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555555"), Name = "Proprietăți & Imobiliare" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555556"), Name = "Financiar & Fiscal" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555557"), Name = "Juridic" },
                    new DocumentCategory { Id = Guid.Parse("11111111-2222-3333-4444-555555555558"), Name = "Altele" }
                };
                await context.DocumentCategories.AddRangeAsync(categorii);
                await context.SaveChangesAsync();
                Console.WriteLine(">>> Categorii documente create cu succes!");
            }

            // =========================================================================
            // 1. SEED PENTRU O INSTITUȚIE DEFAULT (O mutăm PRIMA, ca să existe în DB)
            // =========================================================================
            var defaultInstitutionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            if (!await context.Institutions.AnyAsync(i => i.Id == defaultInstitutionId))
            {
                Console.WriteLine(">>> Seeding Instituție Default...");
                var defaultInstitution = new Institution
                {
                    Id = defaultInstitutionId,
                    Name = "Primăria Centrală (Default)",
                    CUI = "RO000000",
                    Address = "Str. Principală, Nr. 1"
                };
                await context.Institutions.AddAsync(defaultInstitution);
                await context.SaveChangesAsync();
                Console.WriteLine(">>> Instituție Default creată cu succes!");
            }

            // =========================================================================
            // 2. SEED PENTRU DEPARTAMENTE (CompetencyProfiles) - Acum sunt legate de Instituție!
            // =========================================================================
            if (!await context.CompetencyProfiles.AnyAsync())
            {
                Console.WriteLine(">>> Seeding Departamente...");

                var departamente = new List<CompetencyProfile>
                {
                    // MAGIC FIX: Am adăugat InstitutionId = defaultInstitutionId
                    new CompetencyProfile { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), InstitutionId = defaultInstitutionId, Name = "Urbanism", Description = "Emitere certificate urbanism și autorizații." },
                    new CompetencyProfile { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), InstitutionId = defaultInstitutionId, Name = "Taxe și Impozite", Description = "Colectare taxe locale și amenzi." },
                    new CompetencyProfile { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), InstitutionId = defaultInstitutionId, Name = "Stare Civilă", Description = "Evidența populației, căsătorii, nașteri." }
                };

                await context.CompetencyProfiles.AddRangeAsync(departamente);
                await context.SaveChangesAsync();
                Console.WriteLine(">>> Departamente create cu succes!");
            }

            // =========================================================================
            // 3. SEED PENTRU ADMINUL INSTITUȚIEI
            // =========================================================================
            var adminEmail = "admin@local.com";
            var adminExists = await userManager.FindByEmailAsync(adminEmail);

            if (adminExists == null)
            {
                var newAdmin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Super Administrator",
                    Role = UserRole.InstitutionAdmin,
                    InstitutionId = defaultInstitutionId // Îl legăm de instituția de mai sus
                };

                var result = await userManager.CreateAsync(newAdmin, "AdminPass123!");

                if (result.Succeeded)
                {
                    Console.WriteLine(">>> ADMIN CREAT CU SUCCES: admin@local.com / AdminPass123! <<<");
                }
                else
                {
                    Console.WriteLine($">>> EROARE LA CREARE ADMIN: {string.Join(", ", result.Errors.Select(e => e.Description))} <<<");
                }
            }

            // =========================================================================
            // 4. (Opțional) SEED PENTRU SYSADMIN GLOBAL
            // =========================================================================
            var sysAdminEmail = "sysadmin@sistem.ro";
            if (await userManager.FindByEmailAsync(sysAdminEmail) == null)
            {
                var sysAdmin = new AppUser
                {
                    UserName = sysAdminEmail,
                    Email = sysAdminEmail,
                    FullName = "Sys Admin Global",
                    Role = UserRole.SysAdmin,
                    InstitutionId = null // SysAdmin nu aparține de nicio instituție
                };

                var sysResult = await userManager.CreateAsync(sysAdmin, "SysAdmin123!");
                if (sysResult.Succeeded)
                {
                    Console.WriteLine(">>> SYSADMIN CREAT CU SUCCES: sysadmin@sistem.ro / SysAdmin123! <<<");
                }
            }
        }
    }
}