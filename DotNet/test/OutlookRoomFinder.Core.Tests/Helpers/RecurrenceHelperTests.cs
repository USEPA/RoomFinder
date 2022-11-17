using FluentAssertions;
using NSubstitute;
using OutlookRoomFinder.Core.Models.Filter;
using OutlookRoomFinder.Core.Models.Outlook;
using Serilog;
using System;
using System.Linq;
using Xunit;

namespace OutlookRoomFinder.Core.Tests
{
    public class RecurrenceHelperTests
    {
        private static ILogger ilogger;

        public RecurrenceHelperTests()
        {
            ilogger = Substitute.For<ILogger>();
        }

        [Fact]
        public void It_can_convert_durations_timespans()
        {
            // #r "System.xml.System.Xml.Linq"
            var duration = System.Xml.XmlConvert.ToTimeSpan("PT25H");
            duration.Should().Be(new TimeSpan(25, 0, 0));


            var xmlstring = System.Xml.XmlConvert.ToString(new TimeSpan(12, 30, 0));
            xmlstring.Should().Be("PT12H30M");
        }

        [Fact]
        public void It_can_convert_graph_recurrence_dailyExample()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-12-1T14:00:00Z",
                    End = "2019-12-1T14:30:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 1
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Daily,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 30,
                        StartYear = 2019,
                        StartDay = 1,
                        StartMonth = 12,
                        StartTimeMinutes = 0,
                        EndYear = 2019,
                        EndDay = 15,
                        EndMonth = 12
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(30);
            pattern.Periods.Count().Should().Be(15);
        }

        [Fact]
        public void It_can_convert_graph_recurrence_weekly_everyWeek()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-11-18T14:00:00Z",
                    End = "2019-11-18T15:00:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 1,
                        Days = new [] { "mon"},
                        FirstDayOfWeek = "sun"
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Weekly,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 60,
                        StartYear = 2019,
                        StartDay = 18,
                        StartMonth = 11,
                        StartTimeMinutes = -120,
                        EndYear = 2020,
                        EndDay = 3,
                        EndMonth = 2
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(60);
            pattern.Periods.Count().Should().Be(12);
        }

        [Fact]
        public void It_can_convert_graph_recurrence_weekly_everyOtherWeek()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-11-18T14:00:00Z",
                    End = "2019-11-18T15:00:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 2,
                        Days = new[] { "mon" },
                        FirstDayOfWeek = "sun"
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Weekly,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 60,
                        StartYear = 2019,
                        StartDay = 18,
                        StartMonth = 11,
                        StartTimeMinutes = -120,
                        EndYear = 2020,
                        EndDay = 3,
                        EndMonth = 2
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(60);
            pattern.Periods.Count().Should().Be(6);
        }

        [Fact]
        public void It_can_convert_graph_recurrence_weekly_everyOtherWeek_3days()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-11-18T14:00:00Z",
                    End = "2019-11-18T15:00:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 2,
                        Days = new[] { "mon", "thu", "fri" },
                        FirstDayOfWeek = "sun"
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Weekly,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 60,
                        StartYear = 2019,
                        StartDay = 18,
                        StartMonth = 11,
                        StartTimeMinutes = -120,
                        EndYear = 2020,
                        EndDay = 7,
                        EndMonth = 2
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(60);
            pattern.Periods.Count().Should().Be(18); // End Date 2020-2-4" is a Tuesday does not include Last Week as the 3 days are not included
        }

        [Fact]
        public void It_can_convert_graph_recurrence_monthly_everyMonth_firstWeek()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-11-18T14:00:00Z",
                    End = "2019-11-18T14:30:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 1,
                        DayOfWeek = "tue",
                        WeekNumber = Ical.Net.FrequencyOccurrence.First
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Monthly,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 30,
                        StartYear = 2019,
                        StartDay = 18,
                        StartMonth = 11,
                        StartTimeMinutes = -120,
                        EndYear = 2020,
                        EndDay = 18,
                        EndMonth = 11
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(30);
            pattern.Periods.Count().Should().Be(13);
        }

        [Fact]
        public void It_can_convert_graph_recurrence_monthly_everyMonth_On5thDay()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-11-18T14:00:00Z",
                    End = "2019-11-18T14:30:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 1,
                        DayOfMonth = 5,
                        FirstDayOfWeek = "Sun"
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Monthly,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 30,
                        StartYear = 2019,
                        StartDay = 18,
                        StartMonth = 11,
                        StartTimeMinutes = -120,
                        EndYear = 2020,
                        EndDay = 18,
                        EndMonth = 11
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(30);
            pattern.Periods.Count().Should().Be(13);
        }

        [Fact]
        public void It_can_convert_graph_recurrence_yearly_On5thDay()
        {
            var filter = new FindResourceRecurrenceFilter
            {
                SetIndex = 0,
                SetSize = 5,
                RoomFilter = new FindResourceFilter
                {
                    Start = "2019-11-5T14:00:00Z",
                    End = "2019-11-5T14:30:00Z",
                    IncludeUnavailable = true,
                },
                Recurrence = new GraphRecurrence
                {
                    RecurrenceProperties = new GraphRecurrenceProperties
                    {
                        Interval = 0,
                        DayOfMonth = 5,
                        Month = "nov"
                    },
                    RecurrenceTimeZone = new GraphRecurrenceTimeZone
                    {
                        Name = "Pacific Standard Time",
                        Offset = -480
                    },
                    RecurrenceType = Ical.Net.FrequencyType.Yearly,
                    SeriesTime = new GraphRecurrenceSeriesTime
                    {
                        DurationMinutes = 60,
                        StartYear = 2019,
                        StartMonth = 11,
                        StartDay = 5,
                        StartTimeMinutes = -180,
                        EndYear = 2021,
                        EndMonth = 12,
                        EndDay = 31
                    }
                }
            };


            //Arrange
            var helper = new RecurrenceHelper(ilogger);

            // Act
            var pattern = helper.Evaluate(filter, true);

            // Assert
            pattern.DurationMinutes.Should().Be(60);
            pattern.RecurrenceTimeZone.Name.Should().Be("Pacific Standard Time");
            pattern.Periods.Count().Should().Be(3);
        }

    }
}
