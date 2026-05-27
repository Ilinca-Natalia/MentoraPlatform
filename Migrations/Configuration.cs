namespace MentoraPlatform.Migrations
{
    using Microsoft.AspNet.Identity;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Collections.Generic;

    internal sealed class Configuration : DbMigrationsConfiguration<MentoraPlatform.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "MentoraPlatform.Models.ApplicationDbContext";
        }

        protected override void Seed(MentoraPlatform.Models.ApplicationDbContext context)
        {
            var roleManager = new Microsoft.AspNet.Identity.RoleManager<Microsoft.AspNet.Identity.EntityFramework.IdentityRole>(new Microsoft.AspNet.Identity.EntityFramework.RoleStore<Microsoft.AspNet.Identity.EntityFramework.IdentityRole>(context));
            var userManager = new Microsoft.AspNet.Identity.UserManager<MentoraPlatform.Models.ApplicationUser>(new Microsoft.AspNet.Identity.EntityFramework.UserStore<MentoraPlatform.Models.ApplicationUser>(context));

            // 1. Roluri
            if (!roleManager.RoleExists("Admin")) roleManager.Create(new Microsoft.AspNet.Identity.EntityFramework.IdentityRole("Admin"));
            if (!roleManager.RoleExists("Professor")) roleManager.Create(new Microsoft.AspNet.Identity.EntityFramework.IdentityRole("Professor"));
            if (!roleManager.RoleExists("Student")) roleManager.Create(new Microsoft.AspNet.Identity.EntityFramework.IdentityRole("Student"));

            // 2. Admin
            var adminEmail = "admin@mentora.com";
            if (userManager.FindByEmail(adminEmail) == null)
            {
                var user = new MentoraPlatform.Models.ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "System"
                };
                userManager.Create(user, "Admin123!");
                userManager.AddToRole(user.Id, "Admin");
            }

            // 3. SQL Sandbox (Tabelele Marketplace)
            context.Database.ExecuteSqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sandbox_Categorii')
                BEGIN
                    CREATE TABLE Sandbox_Categorii (Id INT PRIMARY KEY IDENTITY(1,1), NumeCategorie NVARCHAR(50));
                    INSERT INTO Sandbox_Categorii (NumeCategorie) VALUES ('Hardware'), ('Software'), ('Accesorii');
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sandbox_Produse')
                BEGIN
                    CREATE TABLE Sandbox_Produse (Id INT PRIMARY KEY IDENTITY(1,1), NumeProdus NVARCHAR(100), Pret DECIMAL(10,2), Stoc INT, CategorieId INT);
                    INSERT INTO Sandbox_Produse (NumeProdus, Pret, Stoc, CategorieId) VALUES 
                    ('Laptop Mentora Pro', 4500.00, 15, 1), ('Mouse Wireless', 120.00, 50, 3), 
                    ('Licenta Mentora OS', 800.00, 100, 2), ('Monitor 4K', 1800.00, 10, 1);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sandbox_Comenzi')
                BEGIN
                    CREATE TABLE Sandbox_Comenzi (Id INT PRIMARY KEY IDENTITY(1,1), DataComanda DATETIME, ClientID INT, StatusComanda NVARCHAR(20));
                    INSERT INTO Sandbox_Comenzi (DataComanda, ClientID, StatusComanda) VALUES 
                    ('2026-05-01', 101, 'Finalizata'), ('2026-05-03', 102, 'In Procesare');
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sandbox_DetaliiComenzi')
                BEGIN
                    CREATE TABLE Sandbox_DetaliiComenzi (Id INT PRIMARY KEY IDENTITY(1,1), ComandaId INT, ProdusId INT, Cantitate INT);
                    INSERT INTO Sandbox_DetaliiComenzi (ComandaId, ProdusId, Cantitate) VALUES (1, 1, 1), (1, 2, 2);
                END
            ");
        }
    }
}