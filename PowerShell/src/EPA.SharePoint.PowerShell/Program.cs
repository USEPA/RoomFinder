using System;

namespace EPA.SharePoint.PowerShell
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "<Pending>")]
    public class Program
    {
        protected Program() { }

        public static int Main(string[] args)
        {
            Console.WriteLine($"Running epa.sharepoint.powershell with args => {args}");
            return 1;
        }
    }
}
