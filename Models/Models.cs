using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelloDoctor.Care.Models
{
    [Table("tbl_district")]
    public class District
    {
        [Key] [Column("did")] public int Did { get; set; }
        [Column("dname")] public string? Dname { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("priority")] public int? Priority { get; set; }
        [Column("userid")] public int Userid { get; set; }
        [Column("updte")] public string? Updte { get; set; }
    }

    [Table("tbl_location")]
    public class Location
    {
        [Key] [Column("cid")] public int Cid { get; set; }
        [Column("cname")] public string? Cname { get; set; }
        [Column("did")] public int? Did { get; set; }
        [ForeignKey("Did")] public virtual District? District { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("priority")] public int Priority { get; set; }
        [Column("userid")] public int Userid { get; set; }
        [Column("updte")] public string? Updte { get; set; }
    }

    [Table("tbl_department")]
    public class Department
    {
        [Key] [Column("depid")] public int Depid { get; set; }
        [Column("depname")] public string? Depname { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("priority")] public int Priority { get; set; }
        [Column("userid")] public int Userid { get; set; }
        [Column("updte")] public string? Updte { get; set; }


    }

    [Table("tbl_doctor")]
    public class Doctor
    {
        [Key] [Column("drid")] public int Drid { get; set; }
        [Column("drname")] public string? Drname { get; set; }
        [Column("depid")] public int? Depid { get; set; }
        [Column("did")] public int? Did { get; set; }
        [Column("cid")] public int? Cid { get; set; }
        [Column("designation")] public string? Designation { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("photo")] public string? Photo { get; set; }
        [Column("payment")] public bool? Payment { get; set; }
        
        [Column("address")] public string? Address { get; set; }
        [Column("workinghospital")] public string? WorkingHospital { get; set; }
        [Column("email")] public string? Email { get; set; }
        [Column("mob1")] public string? Mob1 { get; set; }
        [Column("mob2")] public string? Mob2 { get; set; }
        [Column("updte")] public string? Updte { get; set; }
        [Column("cfee")] public decimal? cfee { get; set; }
        [Column("priority")] public int? Priority { get; set; }
        [ForeignKey("Depid")] public virtual Department? Department { get; set; }
        [ForeignKey("Did")] public virtual District? District { get; set; }
        [ForeignKey("Cid")] public virtual Location? Location { get; set; }
    }
    
    [Table("tbl_livequeue")]
    public class LiveQueue
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.None)] [Column("id")] public int Id { get; set; }
        [Column("drid")] public int? Drid { get; set; }
        [Column("livetoken")] public int? LiveToken { get; set; }
        [Column("updte")] public string? Updte { get; set; }
    }

    [Table("tbl_doctorleave")]
    public class DoctorLeave
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.None)] [Column("lid")] public int Lid { get; set; }
        [Column("drid")] public int? Drid { get; set; }
        [Column("frmdt")] public string? Frmdt { get; set; }
        [Column("todt")] public string? Todt { get; set; }
        [Column("updte")] public string? Updte { get; set; }
    }

    [Table("tbl_consultationdays")]
    public class ConsultationDay
    {
        [Key] [Column("cnid")] public int Cnid { get; set; }
        [Column("dayid")] public int? Dayid { get; set; }
        [Column("userid")] public int? Userid { get; set; }
        [Column("updte")] public string? Updte { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
    }

    [Table("tbl_weekdays")]
    public class Weekday
    {
        [Key] [Column("dayid")] public int Dayid { get; set; }
        [Column("days")] public string? Days { get; set; }
    }

    [Table("tbl_bkngday")]
    public class BookingDay
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.None)] [Column("id")] public int Id { get; set; }
        [Column("daycnt")] public int? Daycnt { get; set; }
        [Column("userid")] public int? Userid { get; set; }
    }

    [Table("tbl_bknghrs")]
    public class BookingHour
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.None)] [Column("id")] public int Id { get; set; }
        [Column("hrcnt")] public int? Hrcnt { get; set; }
        [Column("userid")] public int? Userid { get; set; }
    }

    [Table("tbl_consultationslot")]
    public class ConsultationSlot
    {
        [Key] [Column("slotid")] public int Slotid { get; set; }
        [Column("cnid")] public int? Cnid { get; set; }
        [Column("userid")] public int? Userid { get; set; }
    }

    [Table("tbl_consultationslotdetails")]
    public class ConsultationSlotDetail
    {
        [Key] [Column("slotdtid")] public int Slotdtid { get; set; }
        [Column("slotid")] public int? Slotid { get; set; }
        [Column("frmtymdt")] public string? Frmtymdt { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("token")] public int? Token { get; set; } 
    }

    [Table("tbl_appointment")]
    public class Appointment
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.None)] [Column("aptmtid")] public int Aptmtid { get; set; }
        [Column("pname")] public string? Pname { get; set; }
        [Column("mobnum")] public string? Mobnum { get; set; }
        [Column("depid")] public int? Depid { get; set; }
        [Column("drid")] public int? Drid { get; set; }
        [Column("aptmtdate")] public string? Aptmtdate { get; set; }
        [Column("aptmtday")] public string? Aptmtday { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("payment")] public bool? Payment { get; set; }
        [Column("slotdtid")] public int? Slotdtid { get; set; }
        
        [Column("addr")] public string? Addr { get; set; }
        [Column("age")] public int? Age { get; set; }
        [Column("updte")] public string? Updte { get; set; }
        [Column("aptmtdayid")] public int? Aptmtdayid { get; set; }
    }

    [Table("tbl_roles")]
    public class Role
    {
        [Key] [Column("roleid")] public int Roleid { get; set; }
        [Column("rolename")] public string? Rolename { get; set; }
        [Column("updte")] public string? Updte { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("userid")] public int? Userid { get; set; }
    }

    [Table("tbl_adminuser")]
    public class AdminUser
    {
        [Key] [Column("userid")] public int Userid { get; set; }
        [Column("uname")] public string? Uname { get; set; }
        [Column("pwd")] public string? Pwd { get; set; }
        [Column("email")] public string? Email { get; set; }
        [Column("roleid")] public int? Roleid { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("updte")] public string? Updte { get; set; }
 
        [Column("drid")] public int? Drid { get; set; }
        [Column("subactv")] public bool? Subactv { get; set; }
        [Column("drcode")] public string? Drcode { get; set; }
        [Column("payment")] public bool? Payment { get; set; }
    }

    [Table("tbl_subscriptionplans")]
    public class SubscriptionPlan
    {
        [Key] [Column("pid")] public int Pid { get; set; }
        [Column("pname")] public string? Pname { get; set; }
        [Column("amt")] public string? Amt { get; set; }
        [Column("disc")] public string? Disc { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
        [Column("priority")] public int? Priority { get; set; }
        [Column("updte")] public string? Updte { get; set; }
    }

    [Table("tbl_subscriptiondetails")]
    public class SubscriptionDetail
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.None)] [Column("dtid")] public int Dtid { get; set; }
        [Column("drid")] public int? Drid { get; set; }
        [Column("pid")] public int? Pid { get; set; }
        [Column("subamt")] public decimal? Subamt { get; set; }
        [Column("subfrmdt")] public DateTime? Subfrmdt { get; set; }
        [Column("subtodt")] public DateTime? Subtodt { get; set; }
        [Column("updte")] public string? Updte { get; set; }
        [Column("actv")] public bool? Actv { get; set; }
    }
}
