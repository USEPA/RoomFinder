using FluentAssertions;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using OutlookRoomFinder.Core.Extensions;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.FileModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OutlookRoomFinder.Core.Tests.Helpers
{
    public class ExtensionTests
    {
        public ExtensionTests()
        {
        }

        [Theory]
        [InlineData("ApprovalRequired", RestrictionType.ApprovalRequired)]
        [InlineData("Restricted", RestrictionType.Restricted)]
        [InlineData("Disabled", RestrictionType.Restricted)]
        [InlineData("None", RestrictionType.None)]
        [InlineData("GarbageData", RestrictionType.None)]
        public void It_can_return_proper_ReturnType(string restrictedAttribute, RestrictionType expectedRestrictionType)
        {
            //Arrange
            var resource = new ResourceJsonObject
            {
                DisplayName = "test1",
                RestrictionType = restrictedAttribute
            };

            // Act
            var restrictionType = resource.GetResourceRestriction();

            // Assert
            restrictionType.Should().Be(expectedRestrictionType);
        }

        [Fact]
        public void It_can_convert_empty_array_into_Enumeration()
        {
            // Arrange
            List<string> helloWorld = null;

            // Act
            var emptyEnumeration = helloWorld.ConvertIntoSureEnumerable();

            // Assert
            Assert.NotNull(emptyEnumeration);
            Assert.Empty(emptyEnumeration);
        }

        [Fact]
        public void It_can_convert_DateTimeTimeZone_local_into_utc_validDateTime()
        {
            // Arrange
            var localDateTime = DateTime.Now.AddMinutes(5);
            var localDateTimeTimeZone = localDateTime.ToDateTimeTimeZone(TimeZoneInfo.Local);

            // Act
            var returnLocalDateTimeUtc = localDateTimeTimeZone.ConvertDateToTimeZone(TimeZoneInfo.Local, TimeZoneInfo.Utc);

            // Assert
            Assert.Equal((DateTime.UtcNow.AddMinutes(5)).Hour, returnLocalDateTimeUtc.Hour);
        }

        [Fact]
        public void It_can_convert_DateTimeTimeZone_utc_into_local_validDateTime()
        {
            // Arrange
            var localDateTime = DateTime.Now.AddMinutes(5).ToUniversalTime();
            var localDateTimeTimeZone = localDateTime.ToDateTimeTimeZone(TimeZoneInfo.Utc);

            // Act
            var returnLocalDateTimeUtc = localDateTimeTimeZone.ConvertUtcDateToLocalTimeZone();

            // Assert
            Assert.Equal((DateTime.Now.AddMinutes(5)).Hour, returnLocalDateTimeUtc.Hour);
        }

    }
}
