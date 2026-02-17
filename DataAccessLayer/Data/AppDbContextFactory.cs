using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DataAccessLayer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Data
{
    public class AppDbContextFactory: IDesignTimeDbContextFactory<AppDbContext>
    {  
        public AppDbContext CreateDbContext(string[] args) { 
           
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = "Server=localhost\\SQLEXPRESS;Database=DisertatieDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(connectionString, b=> b.MigrationsAssembly("DataAccessLayer"));
            return new AppDbContext(optionsBuilder.Options);


        }
    }
}
