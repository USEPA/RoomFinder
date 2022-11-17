namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsOffice
    {
        public EzFormsOffice()
        {
            this.aaship = string.Empty;
        }

        public string label { get; set; }

        public string aaship { get; set; }
        
        public bool? isord { get; set; }

        public bool? region { get; set; }
    }
}
