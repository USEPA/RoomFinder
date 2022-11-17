using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.Models.Apps;
using EPA.SharePoint.SysConsole.Models.Governance;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Framework.Provisioning
{
    /// <summary>
    /// Provides common core enumerators for working with the Site Request application
    /// </summary>
    public static class SiteRequestExtensions
    {
        /// <summary>
        /// Load the CollectionSiteTypeLK list into memory
        /// </summary>
        /// <param name="siterequestctx"></param>
        /// <returns></returns>
        public static List<Model_CollectionSiteType> InitCollectionSiteTypeArray(this ClientContext siterequestctx)
        {
            // get site collections
            List _list = siterequestctx.Web.Lists.GetByTitle(Constants_SiteRequest.CollectionSiteTypesLK);
            siterequestctx.Load(_list);
            siterequestctx.ExecuteQueryRetry();


            var _collection = new List<Model_CollectionSiteType>();
            try
            {
                ListItemCollectionPosition itemPosition = null;

                CamlQuery camlQuery = new CamlQuery()
                {
                    ViewXml = CAML.ViewQuery(
                        ViewScope.DefaultValue,
                        string.Empty,
                        string.Empty,
                        CAML.ViewFields((new string[] {
                                Constants_SiteRequest.CollectionSiteTypesLKFields.Field_ID,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.Field_Title,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.FieldBoolean_ShowInDropDown,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteCollection1,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteType,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_CollectionURL,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_Name1,
                                Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_TemplateName
                        }).Select(s => CAML.FieldRef(s)).ToArray()),
                        100)
                };


                while (true)
                {
                    try
                    {
                        camlQuery.ListItemCollectionPosition = itemPosition;
                        ListItemCollection listItems = _list.GetItems(camlQuery);
                        _list.Context.Load(listItems);
                        _list.Context.ExecuteQueryRetry();
                        itemPosition = listItems.ListItemCollectionPosition;

                        foreach (ListItem listItem in listItems)
                        {
                            var sitecollection = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteCollection1);
                            var sitetype = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldLookup_SiteType);
                            var url = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_CollectionURL);

                            var mobj = new Model_CollectionSiteType()
                            {
                                Id = listItem.Id,
                                CollectionURL = url,
                                CollectionURLTrailingSlash = url.EnsureTrailingSlashLowered(),
                                Title = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.Field_Title),
                                TemplateName = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_TemplateName),
                                Name1 = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldText_Name1),
                                ShowInDropDown = listItem.RetrieveListItemValue(Constants_SiteRequest.CollectionSiteTypesLKFields.FieldBoolean_ShowInDropDown).ToBoolean(false),
                                SiteCollection1Id = sitecollection.LookupId,
                                SiteCollection1 = sitecollection.ToLookupValue(),
                                SiteTypeId = sitetype.LookupId,
                                SiteType = sitetype.ToLookupValue()
                            };
                            _collection.Add(mobj);
                            Trace.TraceInformation("collectionsitetype => {0}", mobj);
                        }

                        if (itemPosition == null)
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Failed to extract titles {0}", e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to establish siterequest context {0}", e.Message);
            }

            return _collection;
        }

        /// <summary>
        /// Load up the Site Requests array from the list
        /// </summary>
        /// <param name="siterequestctx">Context for the Site Request</param>
        public static List<Model_SiteRequestItem> InitSiteRequestArray(this ClientContext siterequestctx, List _list = null)
        {
            // get all site request
            if (_list == null || _list?.ServerObjectIsNull == true)
            {
                _list = siterequestctx.Web.Lists.GetByTitle(Constants_SiteRequest.SiteRequestListName);
                siterequestctx.Load(_list);
                siterequestctx.ExecuteQueryRetry();
            }

            var _SiteRequests = new List<Model_SiteRequestItem>();
            try
            {
                var camlViewFields =
                                CAML.ViewFields((new string[] {
                                Constants_SiteRequest.SiteRequestFields.Field_ID,
                                Constants_SiteRequest.SiteRequestFields.Field_Title,
                                Constants_SiteRequest.SiteRequestFields.FieldText_SiteURL,
                                Constants_SiteRequest.SiteRequestFields.FieldText_SiteName,
                                Constants_SiteRequest.SiteRequestFields.FieldText_missingMetaDataId,
                                Constants_SiteRequest.SiteRequestFields.ChoiceField_TypeOfSite,
                                Constants_SiteRequest.SiteRequestFields.FieldText_TypeOfSiteID,
                                Constants_SiteRequest.SiteRequestFields.FieldText_SiteSponsorApprovedFlag,
                                Constants_SiteRequest.SiteRequestFields.FieldText_SiteOwner,
                                Constants_SiteRequest.SiteRequestFields.FieldText_SiteOwnerName,
                                Constants_SiteRequest.SiteRequestFields.FieldText_RequestorEmail,
                                Constants_SiteRequest.SiteRequestFields.FieldText_RequestRejectedFlag,
                                Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag,
                                Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists,
                                Constants_SiteRequest.SiteRequestFields.FieldText_WebGuid
                                }).Select(s => CAML.FieldRef(s)).ToArray());

                var camlWhereClauses = _list.SafeCamlClauseFromThreshold(1000, CAML.Neq(CAML.FieldValue("ContentType", FieldType.Text.ToString("f"), "Folder")));
                camlWhereClauses.ForEach(
                    caml =>
                    {

                        var camlQuery = new CamlQuery()
                        {
                            ViewXml = CAML.ViewQuery(
                                ViewScope.RecursiveAll,
                                (string.IsNullOrEmpty(caml) ? string.Empty : CAML.Where(caml)),
                                string.Empty,
                                camlViewFields,
                                250)
                        };

                        try
                        {
                            while (true)
                            {
                                ListItemCollection listItems = _list.GetItems(camlQuery);
                                siterequestctx.Load(listItems);
                                siterequestctx.ExecuteQueryRetry();

                                foreach (ListItem listItem in listItems)
                                {
                                    var typeofSiteId = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_TypeOfSiteID).ToInt32(0);
                                    var url = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteURL);

                                    var mobj = new Model_SiteRequestItem()
                                    {
                                        Id = listItem.Id,
                                        SiteUrl = url,
                                        SiteUrlTrailingSlash = url.EnsureTrailingSlashLowered(),
                                        Title = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.Field_Title),
                                        SiteName = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteName),
                                        missingMetaDataId = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_missingMetaDataId),
                                        TypeOfSite = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.ChoiceField_TypeOfSite),
                                        TypeOfSiteID = typeofSiteId,
                                        SiteOwner = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteOwner),
                                        SiteOwnerName = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteOwnerName),
                                        SiteSponsorApprovedFlag = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_SiteSponsorApprovedFlag),
                                        RequestCompletedFlag = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestCompletedFlag),
                                        RequestorEmail = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestorEmail),
                                        RequestRejectedFlag = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_RequestRejectedFlag),
                                        WebGuid = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldText_WebGuid),
                                        SiteExists = listItem.RetrieveListItemValue(Constants_SiteRequest.SiteRequestFields.FieldBoolean_SiteExists).ToBoolean(false)
                                    };

                                    _SiteRequests.Add(mobj);
                                    Trace.TraceInformation("siterequest => {0}", mobj);
                                }

                                camlQuery.ListItemCollectionPosition = listItems.ListItemCollectionPosition;
                                if (camlQuery.ListItemCollectionPosition == null)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("Failed to extract site request titles {0}", e.Message);
                        }
                    });
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to establish siterequest context {0}", e.Message);
            }

            return _SiteRequests;
        }

        /// <summary>
        /// Load up the Missing Metadata array from the list
        /// </summary>
        /// <param name="siterequestctx"></param>
        /// <param name="_list">(OPTIONAL) if target list not populated it will load Metadatalist into memory</param>
        public static List<Model_MetadataItem> InitMissingMetadataArray(this ClientContext siterequestctx, List _list = null)
        {
            // get the missing metadata list for further examination
            if (_list == null || _list?.ServerObjectIsNull == true)
            {
                _list = siterequestctx.Web.Lists.GetByTitle(Constants_SiteRequest.MissingMetadataListName);
                siterequestctx.Load(_list, lctx => lctx.ItemCount);
                siterequestctx.ExecuteQueryRetry();
            }

            var missingMetaData = new List<Model_MetadataItem>();
            try
            {
                var camlViewFields = CAML.ViewFields((
                    new string[] {
                        Constants_SiteRequest.MissingMetadataFields.Field_ID,
                        Constants_SiteRequest.MissingMetadataFields.Field_Title,
                        Constants_SiteRequest.MissingMetadataFields.FieldText_SiteTitle,
                        Constants_SiteRequest.MissingMetadataFields.FieldText_HasMetaDataList,
                        Constants_SiteRequest.MissingMetadataFields.FieldLookup_CollectionSiteType,
                        Constants_SiteRequest.MissingMetadataFields.FieldText_EmailSentFlag,
                        Constants_SiteRequest.MissingMetadataFields.FieldBoolean_SiteExists
                    }).Select(s => CAML.FieldRef(s)).ToArray());


                var camlWhereClauses = _list.SafeCamlClauseFromThreshold(1000, CAML.Neq(CAML.FieldValue("ContentType", FieldType.Text.ToString("f"), "Folder")));
                camlWhereClauses.ForEach(
                    caml =>
                    {

                        var camlQuery = new CamlQuery()
                        {
                            ViewXml = CAML.ViewQuery(
                                ViewScope.RecursiveAll,
                                (string.IsNullOrEmpty(caml) ? string.Empty : CAML.Where(caml)),
                                string.Empty,
                                camlViewFields,
                                250)
                        };


                        try
                        {

                            while (true)
                            {
                                ListItemCollection listItems = _list.GetItems(camlQuery);
                                siterequestctx.Load(listItems);
                                siterequestctx.ExecuteQueryRetry();

                                foreach (ListItem listItem in listItems)
                                {
                                    var collectionLookup = listItem.RetrieveListItemValueAsLookup(Constants_SiteRequest.MissingMetadataFields.FieldLookup_CollectionSiteType);
                                    var siteurl = listItem.RetrieveListItemValue(Constants_SiteRequest.MissingMetadataFields.Field_Title);

                                    var mitemobj = new Model_MetadataItem()
                                    {
                                        Id = listItem.Id,
                                        SiteUrl = siteurl,
                                        SiteUrlTrailingSlash = siteurl.EnsureTrailingSlashLowered(),
                                        Title = listItem.RetrieveListItemValue(Constants_SiteRequest.MissingMetadataFields.FieldText_SiteTitle),
                                        HasMetadata = listItem.RetrieveListItemValue(Constants_SiteRequest.MissingMetadataFields.FieldText_HasMetaDataList).ToBoolean(false),
                                        EmailSentFlag = listItem.RetrieveListItemValue(Constants_SiteRequest.MissingMetadataFields.FieldText_EmailSentFlag).ToBoolean(false),
                                        SiteExists = listItem.RetrieveListItemValue(Constants_SiteRequest.MissingMetadataFields.FieldBoolean_SiteExists).ToBoolean(false),
                                        CollectionSiteType = (collectionLookup != null ? collectionLookup.LookupValue : string.Empty),
                                        CollectionSiteTypeId = (collectionLookup != null ? collectionLookup.LookupId : default(Nullable<int>))
                                    };
                                    missingMetaData.Add(mitemobj);
                                    Trace.TraceInformation("metadata => {0}", mitemobj.ToString());
                                }

                                camlQuery.ListItemCollectionPosition = listItems.ListItemCollectionPosition;
                                if (camlQuery.ListItemCollectionPosition == null)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("Failed to extract metadata missing site titles {0}", e.Message);
                        }

                    });
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to pull context {0}", e.Message);
            }

            return missingMetaData;
        }

    }
}
