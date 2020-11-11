﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SACA.Models;
using SACA.Models.Identity;
using System.Reflection;

namespace SACA.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Image> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder
                .UseNpgsql(_configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention();
        }

        //private void UseHiLoStartingSequence(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.HasSequence<int>("DbHiLoSequence").StartsAt(1000).IncrementsBy(1);
        //    NpgsqlModelBuilderExtensions.UseHiLo(modelBuilder, "DbHiLoSequence");
        //}
    }
}
