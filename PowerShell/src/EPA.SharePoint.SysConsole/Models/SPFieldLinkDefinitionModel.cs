using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models
{
    public class SPFieldLinkDefinitionModel
    {
        public System.Guid Id { get; set; }

        public string Name { get; set; }

        public bool Required { get; set; }

        public bool Hidden { get; set; }
    }
}