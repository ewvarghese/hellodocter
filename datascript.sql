USE [docsekpn_hellodoctor]
GO
INSERT [dbo].[tbl_adminuser] ([userid], [uname], [pwd], [email], [roleid], [actv], [updte], [drid], [subactv], [drcode], [payment]) VALUES (1, N'admin', N'K/3Wpm/8l6dHE//GrGiqrw==', N'saranyabhilash@gmail.comm', 1, 1, NULL, NULL, 1, NULL, 0)
GO

INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (1, N'General Medicine (Physician)', 1, 1, 4, N'25-05-2026 12:03:25 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (2, N'Ophthalmology', 8, 1, 1, N'05-09-2020 04:41:27 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (3, N'Gynaecology', 7, 1, 4, N'08-09-2020 06:46:04 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (6, N'ENT', 5, 1, 3, N'07-09-2020 02:46:17 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (7, N'PEDIATRICS', 4, 1, 4, N'08-09-2020 06:45:51 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (8, N'ORTHO', 11, 1, 4, N'08-09-2020 06:46:22 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (9, N'Psychiatry', 9, 1, 4, N'08-09-2020 05:44:34 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (10, N'General Surgery', 3, 1, 4, N'08-09-2020 05:51:45 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (11, N'Respiratory Medicine', 2, 1, 4, N'08-09-2020 05:58:31 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (12, N'General Practitioner', 10, 1, 1, N'12-10-2020 11:55:18 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (13, N'Dermatology', 12, 1, 1, N'21-01-2021 11:42:46 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (14, N'Psychiatry', 13, 1, 1, N'21-01-2021 11:43:04 AM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (15, N'Unani Physician', 14, 1, 1, N'13-12-2024 02:29:53 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (16, N'Medical aesthetics', 15, 1, 1, N'21-01-2026 04:31:43 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (17, N'Ayurvedic Physician', 16, 1, 1, N'09-02-2026 07:46:48 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (18, N'Diabetology', 17, 1, 1, N'16-02-2026 04:20:00 PM')
GO
INSERT [dbo].[tbl_department] ([depid], [depname], [priority], [actv], [userid], [updte]) VALUES (19, N'Orthopedic - Surgeon', 18, 1, 1, N'27-09-2026 09:50:27 PM')
GO
INSERT [dbo].[tbl_district] ([did], [dname], [actv], [updte], [userid], [priority], [img]) VALUES (1, N'Trivandrum', 1, N'25-05-2026 12:03:50 AM', 2, 5, N'തിരുവനന്തപുരം ')
GO
INSERT [dbo].[tbl_district] ([did], [dname], [actv], [updte], [userid], [priority], [img]) VALUES (2, N'Pathanamthitta', 1, N'02-07-2020 02:57:58 PM', 1, 1, N'പത്തനംതിട്ട')
GO
INSERT [dbo].[tbl_district] ([did], [dname], [actv], [updte], [userid], [priority], [img]) VALUES (3, N'Kollam', 1, N'03-07-2020 09:58:32 AM', 1, 3, N'കൊല്ലം')
GO
INSERT [dbo].[tbl_district] ([did], [dname], [actv], [updte], [userid], [priority], [img]) VALUES (4, N'kottayam', 0, N'13-01-2021 11:50:07 AM', 1, 4, N'കോട്ടയം')
GO
INSERT [dbo].[tbl_district] ([did], [dname], [actv], [updte], [userid], [priority], [img]) VALUES (6, N'Wayanad', 1, NULL, 1, 6, N'വയനാട്')
GO
INSERT [dbo].[tbl_district] ([did], [dname], [actv], [updte], [userid], [priority], [img]) VALUES (7, N'Malappuram', 1, N'13-12-2024 12:20:39 PM', 1, 7, N'
മലപ്പുറം')
GO
INSERT [dbo].[tbl_pages] ([pgid], [pgname], [priority], [actv], [linkname], [topactv], [backlink]) VALUES (1, N'Home', 1, 1, N'index', 1, N'home.aspx')
GO
INSERT [dbo].[tbl_pages] ([pgid], [pgname], [priority], [actv], [linkname], [topactv], [backlink]) VALUES (2, N'Departments', 2, 1, N'department', 0, N'department.aspx')
GO
INSERT [dbo].[tbl_pages] ([pgid], [pgname], [priority], [actv], [linkname], [topactv], [backlink]) VALUES (3, N'Doctor', 5, 1, N'doctor', 0, N'doctor.aspx')
GO
INSERT [dbo].[tbl_pages] ([pgid], [pgname], [priority], [actv], [linkname], [topactv], [backlink]) VALUES (5, N'District', 3, 1, N'district', 0, N'district.aspx')
GO
INSERT [dbo].[tbl_pages] ([pgid], [pgname], [priority], [actv], [linkname], [topactv], [backlink]) VALUES (6, N'Location', 4, 1, N'location', 0, N'location.aspx')
GO
INSERT [dbo].[tbl_roles] ([roleid], [rolename], [updte], [actv], [userid]) VALUES (1, N'superadmin', NULL, 0, 1)
GO
INSERT [dbo].[tbl_roles] ([roleid], [rolename], [updte], [actv], [userid]) VALUES (2, N'Admin', NULL, 1, 1)
GO
INSERT [dbo].[tbl_roles] ([roleid], [rolename], [updte], [actv], [userid]) VALUES (3, N'Doctor', NULL, 1, 1)
GO
INSERT [dbo].[tbl_roles] ([roleid], [rolename], [updte], [actv], [userid]) VALUES (4, N'Staff', NULL, 1, 1)
GO
INSERT [dbo].[tbl_staff] ([drid], [drname], [address], [email], [mob1], [actv], [userid], [updte], [priority], [did]) VALUES (2, N'Abin John Mathew', N'Aranmula', N'abin.jmathew@gmail.com', N'9656159322', 1, 1, N'20-01-2021 09:23:23 AM', 2, 2)
GO
INSERT [dbo].[tbl_staff] ([drid], [drname], [address], [email], [mob1], [actv], [userid], [updte], [priority], [did]) VALUES (3, N'Anandhu Krishnan', N'Ranni', N'anandhukrishnan7@gmail.com', N'8547028932', 1, 1, N'22-01-2021 04:34:51 PM', 3, 2)
GO
INSERT [dbo].[tbl_subscriptionplans] ([pid], [pname], [amt], [disc], [actv], [priority], [updte]) VALUES (1, N'Basic', N'500', N'500', 1, 1, NULL)
GO
INSERT [dbo].[tbl_subscriptionplans] ([pid], [pname], [amt], [disc], [actv], [priority], [updte]) VALUES (2, N'Whatsapp', N'800', N'800', 1, 2, NULL)
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (1, N'Sunday')
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (2, N'Monday')
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (3, N'Tuesday')
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (4, N'Wednesday')
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (5, N'Thursday')
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (6, N'Friday')
GO
INSERT [dbo].[tbl_weekdays] ([dayid], [days]) VALUES (7, N'Saturday')
GO
