using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class GroupRoleDefinition
    {
        public GroupRoleDefinition() { }

        public GroupRoleDefinition(string name, RoleType roletype) : this()
        {
            this.name = name;
            this.roletype = roletype;
        }

        public string name { get; set; }

        public int id { get; set; }

        public string role { get; set; }

        public RoleType roletype { get; set; }
    }
}
