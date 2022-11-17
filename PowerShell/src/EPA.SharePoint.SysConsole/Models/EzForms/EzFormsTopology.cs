using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsTopology
    {
        public EzFormsTopology()
        {
            this.TopLevel = new List<string>();
            this.Offices = new List<EzFormsOffice>();
        }

        /// <summary>
        /// Represents the AA-SHIP Level
        /// </summary>
        public List<string> TopLevel { get; set; }

        /// <summary>
        /// Represents the below AA-SHIP Levels
        /// </summary>
        public IList<EzFormsOffice> Offices { get; set; }
    }
}
