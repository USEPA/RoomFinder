using System;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// EZ Forms Request Model
    /// </summary>
    public class EzFormsRequestModel : IEzFormsSPModel, IComparable
    {
        /// <summary>
        /// Initialize entity
        /// </summary>
        public EzFormsRequestModel()
        {
            employee = new EzFormsUserProfileModel();
            manager = new EzFormsUserProfileModel();
        }

        /// <summary>
        /// SharePoint Unique ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Represents the employee for the request
        /// </summary>
        public EzFormsUserProfileModel employee { get; set; }

        /// <summary>
        /// Represents the manager for the employee
        /// </summary>
        public EzFormsUserProfileModel manager { get; set; }

        /// <summary>
        /// Request Type
        /// </summary>
        public string request { get; set; }

        /// <summary>
        /// Request Status
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Request routing type
        /// </summary>
        public string routing { get; set; }

        /// <summary>
        /// Request recertification date if available
        /// </summary>
        public Nullable<DateTime> recertificationDate { get; set; }

        /// <summary>
        /// Requested user computer name
        /// </summary>
        public string computerName { get; set; }

        /// <summary>
        /// Request date was submitted on
        /// </summary>
        public Nullable<DateTime> requestedOn { get; set; }

        /// <summary>
        /// Provides comparable property based on unique SharePoint ID
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            return ID.CompareTo(obj);
        }
    }
}
