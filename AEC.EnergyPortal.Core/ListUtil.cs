using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Security;
using System.Security.Permissions;
using Microsoft.SharePoint;
using System.Xml.Linq;

namespace AEC.EnergyPortal.Core
{
    public class ListUtil
    {
        public static string GetListUrl(string path)
        {
            var firstSlash = path.IndexOf('\\');
            var lastSlash = path.LastIndexOf('\\');
            string retVal = path;

            if (firstSlash > 0 && lastSlash > 0 && lastSlash > firstSlash)
                retVal = path.Substring(firstSlash + 1, lastSlash - firstSlash - 1);

            retVal = retVal.Replace("Views", "Lists");
            return retVal;
        }

        public static string GetListUrl(string webRelativeUrl, string listUrl)
        {
            if (webRelativeUrl[webRelativeUrl.Length - 1] != '/') return (webRelativeUrl + '/' + listUrl);
            else return (webRelativeUrl + listUrl); // Root web case
        }

        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void EnableVersioning(SPWeb web, string listUrl)
        {
            var list = web.GetList(GetListUrl(web.ServerRelativeUrl, listUrl));
            list.EnableVersioning = true;
            list.Update();
        }

        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void EnableContentApproval(SPWeb web, string listUrl)
        {
            var list = web.GetList(GetListUrl(web.ServerRelativeUrl, listUrl));
            list.EnableModeration = true;
            list.DraftVersionVisibility = DraftVisibilityType.Approver;
            list.Update();
        }

        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void RemoveContentTypeFromList(SPWeb web, string listUrl, SPContentTypeId ctId)
        {
            var list = web.GetList(GetListUrl(web.ServerRelativeUrl, listUrl));
            var bestMatch = list.ContentTypes.BestMatch(ctId);

            if (bestMatch.IsChildOf(ctId))
            {
                if (bestMatch.Parent.Equals(ctId))
                {
                    // Is direct descendant
                    list.ContentTypes.Delete(bestMatch);
                }
            }
        }

        public static void UpdateListContentTypes(string webUrl, string listName, SPContentType siteCt)
        {
            using (SPWeb web = new SPSite(webUrl).OpenWeb())
            {
                web.AllowUnsafeUpdates = true;
                SPList list = web.Lists[listName];
                foreach (SPContentType listCt in list.ContentTypes)
                {
                    if (listCt.Id.IsChildOf(siteCt.Id))
                    {
                        // Ensure that all fields in the list CT match the fields in the site CT:
                        //
                        foreach (SPField siteFld in siteCt.Fields)
                        {
                            // Add new fields
                            //
                            if (!listCt.Fields.Contains(siteFld.Id))
                            {
                                try
                                {
                                    // Creating a new SPWeb at this point to avoid save conflicts
                                    //
                                    using (SPWeb nweb = new SPSite(webUrl).OpenWeb())
                                    {
                                        nweb.AllowUnsafeUpdates = true;
                                        SPList nlist = nweb.Lists[listName];
                                        SPContentType nListCt = nlist.ContentTypes[listCt.Id];
                                        // Add the field to the list Content Type
                                        //
                                        SPFieldLink fldLink = new SPFieldLink(siteFld);
                                        nListCt.FieldLinks.Add(fldLink);
                                        nListCt.Update();
                                        nweb.Update();
                                    }
                                }
                                catch { }
                            }
                            else
                            {
                                // Update existing fields
                                //
                                try
                                {
                                    if (list.Fields[siteFld.Id].Group == "Schwab Intranet" && !list.Fields[siteFld.Id].Hidden)
                                    {
                                        // First, delete the Field link from the Content Type
                                        //
                                        listCt.FieldLinks.Delete(siteFld.Id);
                                        listCt.Update();

                                        // Creating a new SPWeb at this point to avoid save conflicts
                                        //
                                        using (SPWeb nweb = new SPSite(webUrl).OpenWeb())
                                        {
                                            nweb.AllowUnsafeUpdates = true;
                                            SPList nlist = nweb.Lists[listName];
                                            SPContentType nListCt = nlist.ContentTypes[listCt.Id];
                                            SPFieldLink fldLink = new SPFieldLink(siteFld);
                                            nListCt.FieldLinks.Add(fldLink);
                                            nListCt.Update();
                                            nweb.Update();
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                web.Update();
                web.AllowUnsafeUpdates = false;
            }
        }

        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void AddViewsToLists(SPFeatureReceiverProperties properties)
        {
            var web = properties.GetWeb();
            var featureElement = properties.Definition.GetXmlDefinition(System.Threading.Thread.CurrentThread.CurrentCulture).GetXElement();
            var elementFiles = featureElement.Descendants().Where(e => e.Name.LocalName == "ElementFile");
            string prevListUrl = string.Empty;

            foreach (var file in elementFiles)
            {
                var viewRelativePath = file.Attribute("Location").Value;
                var listRelativeUrl = GetListUrl(viewRelativePath);
                var viewXmlLocation = properties.Definition.RootDirectory + '\\' + viewRelativePath;
                
                if (viewXmlLocation.Contains("\\Views")){
                    AddViewToList(web, listRelativeUrl, viewXmlLocation);

                    if (listRelativeUrl != prevListUrl)
                    {
                        var list = web.GetList(GetListUrl(web.ServerRelativeUrl, listRelativeUrl));
                        list.Views.Delete(list.Views[0].ID);
                        prevListUrl = listRelativeUrl;
                    }
                }
            }
        }

        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void AddViewsToLists(SPFeatureReceiverProperties properties, string listUrl)
        {
            var web = properties.GetWeb();
            var featureElement = properties.Definition.GetXmlDefinition(System.Threading.Thread.CurrentThread.CurrentCulture).GetXElement();
            var elementFiles = featureElement.Descendants().Where(e => e.Name.LocalName == "ElementFile");

            foreach (var file in elementFiles)
            {
                var viewRelativePath = file.Attribute("Location").Value;
                var viewXmlLocation = properties.Definition.RootDirectory + '\\' + viewRelativePath;
                AddViewToList(web, listUrl, viewXmlLocation);
            }
            var list = web.GetList(GetListUrl(web.ServerRelativeUrl, listUrl));
            list.Views.Delete(list.Views[0].ID);
        }

        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void AddViewToList(SPWeb web, string listUrl, string viewXmlLocation)
        {
            var list = web.GetList(GetListUrl(web.ServerRelativeUrl, listUrl));
            var doc = XDocument.Load(viewXmlLocation);
            var loadedView = new SPView(list, doc.GetXmlDocument());
            var newView = list.Views.Add(
                                            loadedView.Title,
                                            loadedView.ViewFields.ToStringCollection(),
                                            loadedView.Query,
                                            loadedView.RowLimit,
                                            loadedView.Paged,
                                            loadedView.Title == "All Items"  ? loadedView.DefaultView : false
                                         );
            newView.MobileView = loadedView.MobileView;
            newView.MobileDefaultView = loadedView.MobileDefaultView;
            newView.Update();
        }
    }

    public static class ListExtensions
    {
        /// <summary>
        /// Returns a server relative url of the list
        /// </summary>
        /// <param name="typeToTarget"></param>
        /// <returns>string</returns>
        public static string Url(this SPList typeToTarget)
        {
            string listUrl = typeToTarget.DefaultViewUrl;
            if (typeToTarget is SPDocumentLibrary)
            {
                listUrl = listUrl.Remove(listUrl.IndexOf("Forms"));

                if (listUrl.EndsWith("/"))
                {
                    listUrl = listUrl.Remove(listUrl.LastIndexOf("/"));
                }
            }
            else
            {
                int indexSlash = listUrl.LastIndexOf("/");
                listUrl = listUrl.Remove(indexSlash);
            }
            return listUrl;
        }
    } 
}
