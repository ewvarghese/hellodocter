using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelloDoctor.Care.Models
{
    /// <summary>
    /// A medical report written for a patient visit (diagnosis, prescription, advice).
    /// Belongs to a doctor; optionally linked to a booked appointment.
    /// </summary>
    [Table("tbl_patientreport")]
    public class PatientReport
    {
        [Key]
        [Column("rptid")]
        public int Rptid { get; set; }

        [Column("aptmtid")]
        public int? Aptmtid { get; set; }

        [Column("pname")]
        public string? Pname { get; set; }

        [Column("mobnum")]
        public string? Mobnum { get; set; }

        [Column("age")]
        public string? Age { get; set; }

        [Column("addr")]
        public string? Addr { get; set; }

        [Column("drid")]
        public int Drid { get; set; }

        [Column("depid")]
        public int? Depid { get; set; }

        [Column("visitdate")]
        public string? Visitdate { get; set; }

        [Column("diagnosis")]
        public string? Diagnosis { get; set; }

        [Column("prescription")]
        public string? Prescription { get; set; }

        [Column("advice")]
        public string? Advice { get; set; }

        [Column("createddate")]
        public string? Createddate { get; set; }

        [Column("updte")]
        public string? Updte { get; set; }

        [ForeignKey("Drid")]
        public virtual Doctor? Doctor { get; set; }

        public virtual ICollection<PatientReportFile> Files { get; set; } = new List<PatientReportFile>();
    }

    /// <summary>
    /// One row of the patients-per-doctor report list (visit + its reports).
    /// </summary>
    public class PatientVisitRow
    {
        public int Aptmtid { get; set; }
        public string? PatientName { get; set; }
        public string? Mobile { get; set; }
        public string? Age { get; set; }
        public string? Address { get; set; }
        public int Drid { get; set; }
        public string? DoctorName { get; set; }
        public string? VisitDate { get; set; }
        public string? SlotTime { get; set; }
        public string? Token { get; set; }
        public bool Payment { get; set; }
        public List<PatientReport> Reports { get; set; } = new List<PatientReport>();
        public int ReportCount => Reports.Count;
    }

    /// <summary>
    /// A PDF / image file (lab report, scan, ...) uploaded for a patient report.
    /// The file itself is stored under wwwroot/uploads/reports.
    /// </summary>
    [Table("tbl_patientreportfile")]
    public class PatientReportFile
    {
        [Key]
        [Column("fileid")]
        public int Fileid { get; set; }

        [Column("rptid")]
        public int Rptid { get; set; }

        [Column("filename")]
        public string? FileName { get; set; }

        [Column("storedname")]
        public string? StoredName { get; set; }

        [Column("contenttype")]
        public string? ContentType { get; set; }

        [Column("filesize")]
        public long? FileSize { get; set; }

        [Column("uploadeddate")]
        public string? UploadedDate { get; set; }

        [ForeignKey("Rptid")]
        public virtual PatientReport? Report { get; set; }
    }
}
