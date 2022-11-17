using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.REST
{
    public class RestListItemResponseVerboseObject<T> where T : IRestListItemObj
    {
        public List<T> results { get; set; }

        public string __next { get; set; }

    }
}
