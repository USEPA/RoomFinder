using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.Scan
{
    public class ScanCustomActionModel
    {
        public ScanCustomActionModel()
        {
            this.Messages = new ScanResultLogLines();
        }

        public ScanCustomActionModel(UserCustomAction action, AddInScopeEnum scope) : this()
        {
            this.SiteScope = scope;
            this.Id = action.Id;
            this.VersionOfUserCustomAction = action.VersionOfUserCustomAction;
            this.Url = action.Url;
            this.Title = action.Title;
            this.Sequence = action.Sequence;
            this.ScriptSrc = action.ScriptSrc;
            this.Scope = action.Scope;
            this.RegistrationType = action.RegistrationType;
            this.RegistrationId = action.RegistrationId;
            this.Name = action.Name;
            this.Location = action.Location;
            this.ImageUrl = action.ImageUrl;
            this.Group = action.Group;
            this.Description = action.Description;
            this.CommandUIExtension = action.CommandUIExtension;
            this.ClientSideComponentProperties = action.ClientSideComponentProperties;
        }

        public AddInScopeEnum SiteScope { get; set; }

        public string ClientSideComponentProperties { get; set; }

        public string CommandUIExtension { get; set; }

        public string Description { get; set; }

        public string Group { get; set; }

        public Guid Id { get; }

        public string ImageUrl { get; set; }

        public string Location { get; set; }

        public string Name { get; set; }

        public string RegistrationId { get; set; }

        public UserCustomActionRegistrationType RegistrationType { get; set; }

        public UserCustomActionScope Scope { get; }

        public string ScriptSrc { get; set; }

        public int Sequence { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string VersionOfUserCustomAction { get; }

        public ScanResultLogLines Messages { get; set; }
    }
}