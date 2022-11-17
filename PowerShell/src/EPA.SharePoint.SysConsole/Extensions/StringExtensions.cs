using OfficeDevPnP.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Extensions
{
    public static class StringExtensions
    {
        public static MemoryStream GetFileFromStorage(this string fileNameWithPath)
        {
            try
            {
                MemoryStream stream;
                using (FileStream fileStream = File.OpenRead(fileNameWithPath))
                {
                    stream = fileStream.ToMemoryStream();
                }

                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    return null;
                }

                throw;
            }
        }
    }
}
