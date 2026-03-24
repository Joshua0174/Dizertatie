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

            // 1. SEED PENTRU DEPARTAMENTE (CompetencyProfiles)
            if (!await context.CompetencyProfiles.AnyAsync())
            {
                Console.WriteLine(">>> Seeding Departamente...");

                var departamente = new List<CompetencyProfile>
                {
                    new CompetencyProfile { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Urbanism", Description = "Emitere certificate urbanism și autorizații." },
                    new CompetencyProfile { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Taxe și Impozite", Description = "Colectare taxe locale și amenzi." },
                    new CompetencyProfile { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Stare Civilă", Description = "Evidența populației, căsătorii, nașteri." }
                };

                await context.CompetencyProfiles.AddRangeAsync(departamente);
                await context.SaveChangesAsync();
                Console.WriteLine(">>> Departamente create cu succes!");
            }

            // 2. SEED PENTRU O INSTITUȚIE DEFAULT (Necesară pentru noul InstitutionAdmin)
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
            }

            // 3. SEED PENTRU ADMINUL INSTITUȚIEI
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
                    InstitutionId = defaultInstitutionId // <--- Îl legăm de instituția creată mai sus!
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

            // 4. (Opțional) SEED PENTRU SYSADMIN GLOBAL
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
                await userManager.CreateAsync(sysAdmin, "SysAdmin123!");
            }
        }
    }
}