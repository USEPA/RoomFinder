using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsNotifications
    {
        public EzFormsNotifications()
        {
            Notifications = new List<EzFormsNotification>();
            RunOccurrences = new List<DateTime>();
        }

        public List<EzFormsNotification> Notifications { get; set; }

        public List<DateTime> RunOccurrences { get; set; }
    }

    public class EzFormsNotification
    {
        public EzFormsNotification()
        {
            schedule = new List<EzFormsNotifySchedule>();
        }

        public int SPId { get; set; }

        public string email { get; set; }

        public List<EzFormsNotifySchedule> schedule { get; set; }
    }

    public class EzFormsNotifySchedule
    {
        public NotificationType Notification { get; set; }


        public DateTime NotificationDateTime { get; set; }
    }

    public enum NotificationType : Int32
    {
        /// <summary>
        /// placeholder and should not be used
        /// </summary>
        NONE = 0,

        ThirtyDays = 1,

        SevenDays = 2,

        OneDay = 3
    }
}
