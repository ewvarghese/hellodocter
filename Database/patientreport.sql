-- ============================================================
-- patientreport.sql
-- Patient Report feature: medical visit reports + attached files
--
-- Run order: AFTER tablescript.sql (and datascript.sql if wanted)
-- How to run: SSMS -> File -> Open -> this file -> press F5
-- ============================================================

USE [docsekpn_hellodoctor];
GO

-- One row per medical report written for a patient visit
CREATE TABLE [dbo].[tbl_patientreport](
	[rptid] [int] IDENTITY(1,1) NOT NULL,
	[aptmtid] [int] NULL,              -- link to tbl_appointment (NULL = walk-in / manual entry)
	[pname] [nvarchar](200) NULL,      -- patient name
	[mobnum] [nvarchar](50) NULL,
	[age] [nvarchar](20) NULL,
	[addr] [nvarchar](max) NULL,
	[drid] [int] NOT NULL,             -- the doctor this report belongs to (tbl_doctor.drid)
	[depid] [int] NULL,                -- department (tbl_department.depid)
	[visitdate] [nvarchar](50) NULL,   -- visit date, format dd-MMM-yyyy (same as appointments)
	[diagnosis] [nvarchar](max) NULL,
	[prescription] [nvarchar](max) NULL,
	[advice] [nvarchar](max) NULL,
	[createddate] [nvarchar](50) NULL,
	[updte] [nvarchar](50) NULL,
 CONSTRAINT [PK_tbl_patientreport] PRIMARY KEY CLUSTERED
(
	[rptid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- Uploaded files (PDF / images) attached to a report
CREATE TABLE [dbo].[tbl_patientreportfile](
	[fileid] [int] IDENTITY(1,1) NOT NULL,
	[rptid] [int] NOT NULL,            -- belongs to tbl_patientreport.rptid
	[filename] [nvarchar](260) NULL,   -- original file name (shown to the user)
	[storedname] [nvarchar](260) NULL, -- name of the file on disk (wwwroot/uploads/reports)
	[contenttype] [nvarchar](100) NULL,
	[filesize] [bigint] NULL,
	[uploadeddate] [nvarchar](50) NULL,
 CONSTRAINT [PK_tbl_patientreportfile] PRIMARY KEY CLUSTERED
(
	[fileid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
