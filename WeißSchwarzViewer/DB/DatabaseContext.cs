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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dir = "./db";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            optionsBuilder.UseSqlite(
                @$"Data Source={dir}/database.db;");
        }

        
    }
}
