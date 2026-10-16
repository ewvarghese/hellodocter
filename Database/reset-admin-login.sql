-- ============================================================
-- reset-admin-login.sql
-- Fixes "Invalid Username or Password" at the login screen
-- by making sure the seed admin account exists and is intact.
--
-- How to run (beginner steps):
--   1. Open SSMS and connect to the SAME server that
--      appsettings.json points to (the "Server=..." part).
--   2. File -> Open -> File... -> select this file -> Open
--   3. Press F5 (Execute).
--   4. At the bottom, "Results" should show one row:
--        userid=1, uname=admin, pwd=K/3Wpm/8l6dHE//GrGiqrw==
--   5. Log in to the app with:  admin / adhir@9856
--      (type it manually — do not copy-paste)
--
-- What it does:
--   * Removes any broken/duplicate admin rows
--   * Re-inserts the seed admin account with the correct
--     encrypted password ([pwd] is the app's DES-encrypted
--     form of the password — never edit it by hand)
-- ============================================================

USE [docsekpn_hellodoctor];
GO

DELETE FROM [dbo].[tbl_adminuser] WHERE [userid] = 1 OR [uname] = N'admin';
GO

INSERT INTO [dbo].[tbl_adminuser] ([userid], [uname], [pwd], [email], [roleid], [actv], [updte], [drid], [subactv], [drcode], [payment])
VALUES (1, N'admin', N'K/3Wpm/8l6dHE//GrGiqrw==', N'saranyabhilash@gmail.comm', 1, 1, NULL, NULL, 1, NULL, 0);
GO

-- Verify (you should see exactly one admin row):
SELECT [userid], [uname], [pwd] FROM [dbo].[tbl_adminuser];
GO
