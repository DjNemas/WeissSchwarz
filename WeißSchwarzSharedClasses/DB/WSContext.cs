using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeißSchwarzSharedClasses.Models;

namespace WeißSchwarzSharedClasses.DB
{
    public class WSContext : DbContext
    {
        public DbSet<Set> Sets { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Trigger> Trigger { get; set; }
        public DbSet<Trait> Traits { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("config.json");
            var config = builder.Build();
            string test = config.GetConnectionString("WSDB");
            optionsBuilder.UseMySQL(config.GetConnectionString("WSDB"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Enum Converter Set
            modelBuilder.Entity<Set>()
                .Property(x => x.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (SetType)Enum.Parse(typeof(SetType), v));

            // Enum Converter Card
            modelBuilder.Entity<Card>()
                .Property(x => x.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (CardType)Enum.Parse(typeof(WeißSchwarzSharedClasses.Models.CardType), v));

            modelBuilder.Entity<Card>()
                .Property(x => x.Color)
                .HasConversion(
                    v => v.ToString(),
                    v => (Color)Enum.Parse(typeof(Color), v));

            modelBuilder.Entity<Card>()
                .Property(x => x.Side)
                .HasConversion(
                    v => v.ToString(),
                    v => (Side)Enum.Parse(typeof(Side), v));

            // Enum Converter Trigger
            modelBuilder.Entity<Trigger>()
                .Property(x => x.TriggerType)
                .HasConversion(
                    v => v.ToString(),
                    v => (TriggerType)Enum.Parse(typeof(TriggerType), v));
        }
    }
}
