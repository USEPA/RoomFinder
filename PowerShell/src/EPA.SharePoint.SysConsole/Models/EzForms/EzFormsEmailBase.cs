using Newtonsoft.Json;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsEmailBase
    {
        /// <summary>
        /// The unique id of the request form
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The users email address
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Date the workflow terminated
        /// </summary>
        public string DateOfLastModification { get; set; }

        /// <summary>
        /// Link to View the form
        /// </summary>
        [JsonIgnore()]
        public string EmailViewUrl
        {
            get
            {
                return string.Format("SitePages/View%20Request.aspx?requestID={0}&Source=%2fsites%2fEZForms%2fSitePages%2fMy%20Requests.aspx", this.Id);
            }
        }

        /// <summary>
        /// Link to edit the form
        /// </summary>
        [JsonIgnore()]
        public string EmailEditUrl
        {
            get
            {
                return string.Format("SitePages/Edit%20Request.aspx?requestID={0}&Source=%2fsites%2fEZForms%2fSitePages%2fMy%20Requests.aspx", this.Id);
            }
        }

        /// <summary>
        /// Format the url 
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <returns></returns>
        public string GetViewUrl(string siteUrl)
        {
            return string.Format("{0}/{1}", siteUrl, this.EmailViewUrl);
        }

        /// <summary>
        /// override the object to string and return a url
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.EmailViewUrl;
        }
    }
}
