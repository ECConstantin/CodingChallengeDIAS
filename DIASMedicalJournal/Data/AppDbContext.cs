using DIASMedicalJournal.Models;
using Microsoft.EntityFrameworkCore;

namespace DIASMedicalJournal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Admission> Admissions { get; set; }

    }
}
