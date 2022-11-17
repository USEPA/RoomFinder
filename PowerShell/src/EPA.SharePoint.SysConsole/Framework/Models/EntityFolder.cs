using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Framework.Models
{
    public class EntityFolder
    {
        public string Url { get; set; }

        public List<EntityFile> Files { get; set; }
    }
}
