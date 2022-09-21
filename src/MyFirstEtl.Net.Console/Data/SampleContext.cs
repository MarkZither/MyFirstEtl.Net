using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace MyFirstEtl.Net.Console.Data
{
    public class SampleContext : DbContext
    {
        public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source=temp.db");
    }
}
