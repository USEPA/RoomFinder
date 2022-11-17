CREATE PROCEDURE [dbo].[MSTeams_01_UsageMonthlyDates] AS
BEGIN

WITH UserActivity AS
(
select 
	datepart(YYYY, LastActivityDate) lastyear,
	datepart(MM, LastActivityDate) lastmonth,
	datepart(DD, LastActivityDate) lastday,
	LastActivityDate
from 
	GraphTeamsActivityUserDetail gdet
),

UserRollup AS 
(
SELECT
	MIN(gsum.LastActivityDate) FirstDate,
	MAX(gsum.LastActivityDate) LastDate,
	count(distinct gsum.lastday) TotalMetricDays
FROM	
	UserActivity gsum
GROUP BY
	gsum.lastyear, gsum.lastmonth
),

UserDates AS
(
SELECT
	FirstDate, LastDate, TotalMetricDays,
	RIGHT('00' + CONVERT(VARCHAR(2), datepart(MM, FirstDate)), 2) AS MonthDisplay,
	RIGHT('0000' + datepart(YYYY, FirstDate), 4) AS YearDisplay,
	RIGHT('00' + CONVERT(VARCHAR(2), datepart(MM, FirstDate)), 2) + '/01/' + RIGHT('0000' + datepart(YYYY, FirstDate), 4) FormattedDate
FROM
	UserRollup GROL
)


	-- establish the target or destination table
	MERGE INTO [dbo].[O365PreviewTeamsDates] AS odsm
	USING (
		SELECT TOP 100 PERCENT 
		FirstDate, LastDate, TotalMetricDays, MonthDisplay, YearDisplay, FormattedDate
		FROM UserDates AS odb 
		ORDER BY odb.FirstDate
	) AS odb on odb.FormattedDate = odsm.FormattedDate

		-- IF No Row, Add
	WHEN NOT MATCHED BY TARGET THEN
	INSERT ( 
		FormattedDate, DTSTART, DTEND, TotalMetricDays, RequiresCalculation
	)
	VALUES (
		FormattedDate, FirstDate, LastDate, TotalMetricDays, 1
	)
	-- Update the Values
	WHEN MATCHED AND DTEND <> LastDate THEN
		UPDATE SET
		odsm.DTEND = odb.LastDate,
		odsm.TotalMetricDays = odb.TotalMetricDays,
		odsm.RequiresCalculation = 1
		-- Double Check the Query and Updates
	OUTPUT 
		$ACTION,
		ISNULL(inserted.FormattedDate, deleted.FormattedDate) as FormattedDate, 
		ISNULL(inserted.DTEND, deleted.DTEND) as DTEND, 
		SUSER_SNAME() + ' ' +$action AS logAction,
		GETDATE() AS logDate;

END
GO


