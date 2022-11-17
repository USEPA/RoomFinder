using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.Extensions;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.Filter;
using OutlookRoomFinder.Core.Models.Outlook;
using OutlookRoomFinder.Core.Services;
using OutlookRoomFinder.Web.Controllers;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OutlookRoomFinder.Web.Tests
{
    public class ODataRoomServiceControllerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static IAppSettings appSettings;
        private static IExchangeContext exchangeContext;
        private static IExchangeService exchangeService;

        private static readonly List<ResourceItemMailbox> arrangedRooms = new List<ResourceItemMailbox> {
                new ResourceItemMailbox {
                    RoomList = new ADEntry { EmailAddress = "roomlist1@somewhere.net", DisplayName = "roomlist1" },
                    DisplayName = "room1",
                    Location = new LocationAddress { State = "location1" },
                    EmailAddress = "room1@contoso.com",
                    Capacity = 2
                },
                new ResourceItemMailbox {
                    RoomList = new ADEntry { EmailAddress = "roomlist2@somewhere.net", DisplayName = "roomlist2" },
                    DisplayName = "room2",
                    Location = new LocationAddress { State = "location1" },
                    EmailAddress = "room2@contoso.com",
                    Capacity = 25
                }
            };

        public ODataRoomServiceControllerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private static ODataRoomService BuildController()
        {
            var ilogger = Substitute.For<ILogger>();
            appSettings = Substitute.For<IAppSettings>();
            appSettings.Exchange.Returns(new AppSettingsExchange
            {
                DefaultRoomCapacity = 1
            });
            exchangeContext = Substitute.For<IExchangeContext>();
            exchangeContext.GetRooms().Returns(new ConcurrentBag<ResourceItemMailbox>(arrangedRooms.ToArray()));

            exchangeService = Substitute.For<IExchangeService>();
            exchangeService.GetAvailabilityAsync(Arg.Any<IEnumerable<IResourceItem>>(), Arg.Any<MeetingTimeWindow>(), true)
                .Returns(arrangedRooms.ToArray<IResourceItem>());

            var it = new ODataRoomService(ilogger, appSettings, exchangeContext, exchangeService);
            return it;
        }

        private ODataRoomService BuildControllerWithFileContext()
        {
            var loggerConfig = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.TestOutput(_testOutputHelper).CreateLogger();

            appSettings = Substitute.For<IAppSettings>();
            appSettings.WebRootPath = Directory.GetCurrentDirectory();
            appSettings.Exchange = new AppSettingsExchange
            {
                JsonFilename = "resources-objects.json",
                DefaultRoomCapacity = 1,
                CacheLifetime = 1440
            };
            exchangeContext = Substitute.For<IExchangeContext>();
            exchangeContext = new JsonExchangeContext(loggerConfig, appSettings);
            exchangeContext.ClearCache(); // Static [across all threads]

            exchangeService = Substitute.For<IExchangeService>();
            exchangeService.GetAvailabilityAsync(Arg.Any<IEnumerable<IResourceItem>>(), Arg.Any<MeetingTimeWindow>(), true)
                .Returns(arrangedRooms.ToArray<IResourceItem>());

            var it = new ODataRoomService(loggerConfig, appSettings, exchangeContext, exchangeService);
            return it;
        }

        [Fact]
        public void It_can_query_get_conference_rooms_and_Return_Ok()
        {
            //Arrange
            using var it = BuildController();

            // Act
            var result = it.RoomListRooms("roomlist1");

            // Assert
            var okresult = result as OkObjectResult;
            var rooms = okresult?.Value as IEnumerable<ResourceItemMailbox>;
            rooms?.Count().Should().Be(1);
        }

        [Fact]
        public async Task It_can_filter_conference_rooms_and_Return_Ok()
        {
            using var it = BuildController();

            var filter = new FindResourceFilter
            {
                Start = DateTime.UtcNow.AddMinutes(30).ToString("s"),
                End = DateTime.UtcNow.AddMinutes(45).ToString("s"),
                IncludeRestricted = true,
                Capacity = 2
            };

            var result = await it.Find(filter);

            result.Should().BeOfType(typeof(OkObjectResult));
            result.Should().NotBeNull();
        }

        [Fact]
        public void It_can_return_graph_recurrence_dailyExample_OkRequest()
        {
            var model = new FindResourceRecurrenceFilter
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
            using var it = BuildController();

            // Act
            var result = it.CheckRecurrence(model);

            // Assert
            var okresult = result as OkObjectResult;
            var pattern = okresult?.Value as GraphRecurrencePattern;


            pattern.DurationMinutes.Should().Be(30);
            pattern.Periods.Count().Should().Be(15);
        }


        [Fact]
        public void It_can_load_jsonFile_and_return_states_OkRequest()
        {
            //Arrange
            using var it = BuildControllerWithFileContext();

            // Act
            var result = it.States();

            // Assert
            var okresult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okresult?.Value as IList<string>;
            resultValue?.Count().Should().Be(1);
        }

        [Theory]
        [InlineData("OH", typeof(OkObjectResult), false, "")]
        [InlineData("FL", typeof(BadRequestObjectResult), false, "Failed to find cities in FL.")]
        [InlineData("", typeof(BadRequestObjectResult), true, "RoomDataService.Officees:  State should not be null")]
        public void It_can_load_jsonFile_and_return_cities_OkRequest(string stateText, Type expectedType, bool serializableError, string expectedMessage)
        {
            //Arrange
            using var it = BuildControllerWithFileContext();

            // Act
            var result = it.Cities(stateText);

            // Assert
            result.Should().BeAssignableTo(expectedType);
            if (expectedType == typeof(OkObjectResult))
            {
                var okresult = Assert.IsType<OkObjectResult>(result);
                var resultValue = okresult?.Value as IList<string>;
                resultValue?.Count().Should().Be(1);
            }

            if (expectedType == typeof(BadRequestObjectResult))
            {
                var badResult = ((BadRequestObjectResult)result).Value;
                if (serializableError)
                {
                    badResult.Should().BeAssignableTo<SerializableError>();
                    var badResultError = (SerializableError)badResult;
                    badResultError.TryGetValue("state", out object errorMessage);
                    errorMessage.Should().BeAssignableTo<IEnumerable<string>>();
                    (errorMessage as IEnumerable<string>).FirstOrDefault().Should().Be(expectedMessage);
                }
                else
                {
                    badResult.Should().Be(expectedMessage);
                }
            }
        }

        [Theory]
        [InlineData("OH", "Cincinnati", typeof(OkObjectResult), "", "")]
        [InlineData("OH", "Bengals", typeof(BadRequestObjectResult), "", "Failed to find offices in Bengals, OH.")]
        [InlineData("", "", typeof(BadRequestObjectResult), "state", "RoomDataService.Officees:  State should not be null")]
        [InlineData("OH", "", typeof(BadRequestObjectResult), "city", "RoomDataService.Officees:  City should not be null")]
        public void It_can_load_jsonFile_and_return_offices_OkRequest(string stateText, string cityText, Type expectedType, string serializableErrorKey, string expectedMessage)
        {
            //Arrange
            using var it = BuildControllerWithFileContext();

            // Act
            var result = it.Officees(stateText, cityText);

            // Assert
            result.Should().BeAssignableTo(expectedType);
            if (expectedType == typeof(OkObjectResult))
            {
                var okresult = Assert.IsType<OkObjectResult>(result);
                var resultValue = okresult?.Value as IList<string>;
                resultValue?.Count().Should().Be(3);
            }

            if (expectedType == typeof(BadRequestObjectResult))
            {
                var badResult = ((BadRequestObjectResult)result).Value;
                if (!string.IsNullOrEmpty(serializableErrorKey))
                {
                    badResult.Should().BeAssignableTo<SerializableError>();
                    var badResultError = (SerializableError)badResult;
                    badResultError.TryGetValue(serializableErrorKey, out object errorMessage);
                    errorMessage.Should().BeAssignableTo<IEnumerable<string>>();
                    (errorMessage as IEnumerable<string>).FirstOrDefault().Should().Be(expectedMessage);
                }
                else
                {
                    badResult.Should().Be(expectedMessage);
                }
            }
        }
    }
}
