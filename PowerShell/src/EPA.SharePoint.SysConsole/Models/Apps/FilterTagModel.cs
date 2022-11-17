namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class FilterTagModel
    {
        public FilterTagModel()
        {
            this.selected = false;
        }

        public string display { get; set; }

        public string name { get { return display.ToLower(); } }


        public bool selected { get; set; }


        public string tagName { get { return string.Format("tag:{0}", name); } }
    }
}