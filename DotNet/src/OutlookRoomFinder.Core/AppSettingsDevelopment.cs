using System;
using System.Diagnostics.CodeAnalysis;

namespace OutlookRoomFinder.Core
{
    public class AppSettingsDevelopment
    {
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public string[] Cors { get; set; } = Array.Empty<string>();
    }
}
