using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models
{
    public class CollectionModel
    {
        /// <summary>
        /// Absolute URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Total subweb count
        /// </summary>
        public int WebsCount { get; set; }

        /// <summary>
        /// emit URLs
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Url;
        }
    }
}