namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// Departments | Offices at the EPA
    /// </summary>
    public class EZFormsDepartments
    {
        public EZFormsDepartments() { }

        /// <summary>
        /// Unique Identifier
        /// </summary>
        public string DepartmentNumber { get; set; }

        /// <summary>
        /// Org Code
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// Full Name
        /// </summary>
        public string OrgName { get; set; }

        /// <summary>
        /// Level in the Heirarchy
        /// </summary>
        public int OrgLevel { get; set; }

        /// <summary>
        /// Parent ORG
        /// </summary>
        public string ParentOrgCode { get; set; }
    }
}
