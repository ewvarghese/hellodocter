using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Models;

namespace HelloDoctor.Care.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<District> Districts { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<LiveQueue> LiveQueues { get; set; }
        public DbSet<DoctorLeave> Leaves { get; set; }
        
        public DbSet<ConsultationDay> ConsultationDays { get; set; }
        public DbSet<Weekday> Weekdays { get; set; }
        
        public DbSet<BookingDay> BookingDays { get; set; }
        public DbSet<BookingHour> BookingHours { get; set; }
        
        public DbSet<ConsultationSlot> ConsultationSlots { get; set; }
        public DbSet<ConsultationSlotDetail> ConsultationSlotDetails { get; set; }
        
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<SubscriptionDetail> SubscriptionDetails { get; set; }
    }
}
