-- =======================================================
-- Author:		James Hunter
-- Create date: 11/6/2019
-- Description:	Purge the ETL.eBusinessAccounts table
-- =======================================================
CREATE PROCEDURE [dbo].[sp_del_etlEBusinessAccounts] 
AS
BEGIN
	DELETE FROM etl.eBusinessAccounts
END
GO


