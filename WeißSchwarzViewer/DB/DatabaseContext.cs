using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeißSchwarzSharedClasses.Models;

namespace WeißSchwarzViewer.DB
{
    public class DatabaseContext : DbContext
    {
        public static DatabaseContext DB = new DatabaseContext();
        public DbSet<Set> Sets { get; set; }
        public DbSet<LocalDataVersion> DataVersion { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dir = "./db";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            optionsBuilder.UseSqlite(
                @$"Data Source={dir}/database.db;");
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
                    v => (CardType)Enum.Parse(typeof(CardType), v));

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

        public class LocalDataVersion
        {
            public int ID { get; set; }
            public int Version { get; set; }
        }
    }
}
