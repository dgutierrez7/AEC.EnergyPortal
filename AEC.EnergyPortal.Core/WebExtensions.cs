using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Taxonomy;
using Microsoft.Office.Server.Utilities;
using System.Reflection;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Publishing;
using System.Xml;

namespace AEC.EnergyPortal.Core
{
    public static class WebExtensions
    {
        public static string GetSafeName(string inName)
        {
            // first trim the raw string    
            string safe = inName.Trim();

            if (safe.Length <= 0)   // no need to check anymore
                return safe;

            // replace spaces with hyphens    
            safe = safe.Replace(" ", "-").ToLower();

            // trim out illegal characters    
            safe = System.Text.RegularExpressions.Regex.Replace(safe, "[^a-z0-9\\-]", "");

            // replace any 'double spaces' with singles    
            if (safe.IndexOf("--") > -1)
                while (safe.IndexOf("--") > -1)
                    safe = safe.Replace("--", "-");

            // trim the length    
            if (safe.Length > 50)
                safe = safe.Substring(0, 49);

            // clean the beginning and end of the filename    
            char[] replace = { '-', '.' };
            safe = safe.TrimStart(replace);
            safe = safe.TrimEnd(replace);
            return safe;
        }

        public static PublishingWeb ProvisionPublishingWeb(SPWeb web, string masterPageName)
        {
            if (web == web.Site.RootWeb)
            {
                web.CustomMasterUrl = (web.ServerRelativeUrl.Length == 1 ? web.ServerRelativeUrl : web.ServerRelativeUrl + "/") 
                    + "_catalogs/masterpage/" + masterPageName;
            }

            var pWeb = PublishingWeb.GetPublishingWeb(web);
            pWeb.Navigation.CurrentIncludePages = pWeb.Navigation.CurrentIncludeSubSites = true;
            pWeb.Navigation.ShowSiblings = pWeb.Navigation.InheritCurrent = false;
            pWeb.Update();
            return pWeb;
        }

        public static SPWeb GetWeb(this SPFeatureReceiverProperties props)
        {
            var web = props.Feature.Parent as SPWeb;

            if (web == null)
                web = ((SPSite)props.Feature.Parent).RootWeb;

            return web;
        }

        public static SPSite GetSite(this SPFeatureReceiverProperties props)
        {
            var web = props.Feature.Parent as SPWeb;
            var site = props.Feature.Parent as SPSite;

            if (web != null)
                return web.Site;

            return site;
        }

        public static Group GetGroup(this TaxonomySession session, string termStoreName, string groupName)
        {
            if (session.TermStores.Count != 0)
            {
                var termStore = session.TermStores.FirstOrDefault();
                var group = termStore.Groups.FirstOrDefault(g => g.Name == groupName);
                return group;
            }

            return null;
        }

        public static TermSet GetTermSet(this TaxonomySession session, string termStoreName, string groupName, string termSetName)
        {
            var group = GetGroup(session, termStoreName, groupName);

            if (group != null)
            {
                return group.TermSets.FirstOrDefault(t => t.Name == termSetName);
            }
            return null;
        }

        public static Term GetTerm(this TaxonomySession session, string termStoreName, string groupName, string termName)
        {
            var termSet = GetTermSet(session, termStoreName, groupName, groupName);

            if (termSet != null)
            {
                return termSet.Terms.FirstOrDefault(t => t.Name == termName);
            }
            return null;
        }

        public static Term GetTerm(this TaxonomySession session, string termStoreName, string groupName, string termsetName, string termName)
        {
            var termSet = GetTermSet(session, termStoreName, groupName, termsetName);

            if (termSet != null)
            {
                return termSet.Terms.FirstOrDefault(t => t.Name == termName);
            }
            return null;
        }

        public static Term GetTerm(this Term term, string childTermName)
        {
            if (term != null)
            {
                return term.Terms.FirstOrDefault(t => t.Name == childTermName);
            }
            return null;
        }

        public static SPSite UpdateTaxonomyField(this SPSite site, string fieldId, string textFieldId, string termName)
        {
            UpdateTaxonomyField(site, fieldId, textFieldId, Globals.TermGroups.EnterpriseTaxonomy, Globals.TermSets.EnterpriseTaxonomy, termName, false);
            return site;
        }

        public static SPSite UpdateTaxonomyField(this SPSite site, string fieldId, string textFieldId, string termGroup, string termsetName, string termName, bool createValuesInEditForm)
        {
            var taxonomyFieldId = new Guid(fieldId);
            var session = new TaxonomySession(site);
            var term = session.GetTerm
                            (
                                Globals.TermStore,
                                termGroup,
                                termsetName,
                                termName
                            );
            var taxonomyField = site.RootWeb.Fields[taxonomyFieldId] as TaxonomyField;
            if (taxonomyField != null)
            {
                taxonomyField.SspId = term.TermStore.Id;
                taxonomyField.TermSetId = term.TermSet.Id;
                taxonomyField.TargetTemplate = string.Empty;
                taxonomyField.AnchorId = term.Id;
                taxonomyField.TextField = new Guid(textFieldId);
                taxonomyField.CreateValuesInEditForm = createValuesInEditForm;
                taxonomyField.Update(true);
            }
            return site;
        }
        public static SPSite UpdateLookupField(this SPSite site, string fieldId, string listUrl, string displayField)
        {
            var web = site.RootWeb;
            var lookupField = web.Fields[new Guid(fieldId)] as SPFieldLookup;
            lookupField.LookupWebId = web.ID;
            lookupField.LookupList = site.RootWeb.GetList(ListUtil.GetListUrl(web.ServerRelativeUrl, listUrl)).ID.ToString();
            lookupField.LookupField = displayField;
            lookupField.Update(true);
            return site;
        }

        public static int[] GetWssIdsOfTerm(this SPSite site, string termSetName, string termName)
        {
            var session = new TaxonomySession(site);
            var term = session.GetTerm
                            (
                                Globals.TermStore,
                                Globals.TermGroups.EnterpriseTaxonomy,
                                termSetName
                            );
            var childTerm = term.GetTerm(termName);

            if (childTerm != null)
            {
                var wssIds = TaxonomyField.GetWssIdsOfTerm(site, childTerm.TermStore.Id, childTerm.TermSet.Id, childTerm.Id, false, 1);
                return wssIds;
            }

            return new int[0];
        }

        public static int[] GetWssIdsOfTerm(SPSite site, string groupName, string termSetName, string termName)
        {
            var term = GetTerm(site, groupName, termSetName, null, termName);

            if (term != null)
            {
                var wssIds = TaxonomyField.GetWssIdsOfTerm(site, term.TermStore.Id, term.TermSet.Id, term.Id, false, 1);
                return wssIds;
            }

            return new int[0];
        }

        public static Term GetTerm(SPSite site, string groupName, string termSetName, string parentTermName, string termName)
        {
            Term newTerm = null;
            SPSecurity.RunWithElevatedPrivileges(() =>
            {
                using (var site2 = new SPSite(site.ID, site.Zone))
                {
                    TaxonomySession session = new TaxonomySession(site2);
                    var termStore = session.TermStores[0];
                    var group = (from g in termStore.Groups
                                 where g.Name == groupName
                                 select g
                                ).FirstOrDefault();

                    var termSet = group.TermSets[termSetName];

                    if (termSet != null)
                    {
                        var term = (from t in termSet.GetAllTerms()
                                    where t.Name.Replace((char)65286, '&').Equals(termName.Replace((char)65286, '&'), StringComparison.OrdinalIgnoreCase)
                                    select t).FirstOrDefault();

                        if (term == null)
                        {
                            var parentTerm = (from t in termSet.GetAllTerms()
                                              where t.Name.Replace((char)65286, '&').Equals(parentTermName.Replace((char)65286, '&'), StringComparison.OrdinalIgnoreCase)
                                              select t).FirstOrDefault();
                            if (parentTerm != null)
                            {
                                newTerm = parentTerm.CreateTerm(termName, site.RootWeb.Locale.LCID);
                                termStore.CommitAll();
                            }
                            else
                            {
                                newTerm = termSet.CreateTerm(termName, site.RootWeb.Locale.LCID);
                                termStore.CommitAll();
                            }
                        }
                        else
                            newTerm = term;
                    }
                }
            });
            return newTerm;
        }

        public static void ProcessItems(this SPList list, SPQuery query, ContentIterator.ItemProcessor itemProcesser, ContentIterator.ItemProcessorErrorCallout errorCallout)
        {
            ContentIterator ci = new ContentIterator();
            ci.ProcessListItems(list, query, itemProcesser, errorCallout);
        }

        public static void ProcessItems(this SPList list, SPQuery query, ContentIterator.ItemsProcessor itemsProcesser, ContentIterator.ItemsProcessorErrorCallout errorCallout)
        {
            ContentIterator ci = new ContentIterator();
            ci.ProcessListItems(list, query, itemsProcesser, errorCallout);
        }

        public static void ProcessItem(this SPListItemCollection items, ContentIterator.ItemProcessor itemProcesser, ContentIterator.ItemProcessorErrorCallout errorCallout)
        {
            ContentIterator ci = new ContentIterator();
            ci.ProcessItems(items, itemProcesser, errorCallout);
        }

        public static IEnumerable<string> Tokenize(this string src, string seperator)
        {
            var results = src.Split(new string[] { seperator }, StringSplitOptions.None);

            foreach (var result in results)
                yield return result;
        }


        public static void SetFieldAttribute(SPField field, string attribute, string value)
        {
            Type type = field.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo mi = type.GetMethod("SetFieldAttributeValue", flags);
            mi.Invoke(field, new object[] { attribute, value });
        }

        public static string GetFieldAttribute(SPField field, string attribute)
        {
            Type type = field.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo mi = type.GetMethod("GetFieldAttributeValue", flags, null, new Type[] { typeof(String) }, null);
            object obj = mi.Invoke(field, new object[] { attribute });

            if (obj == null)
                return "";
            else
                return obj.ToString();
        }

        public static T IsNull<T>(object src, T defaultValue)
        {
            if (src == null)
                return defaultValue;
            T retVal;

            try
            {
                retVal = (T)src;
            }
            catch
            {
                object o = src.ToString();
                retVal = (T)o;
            }

            return retVal;
        }

        public static IDictionary<int, string> GetEnumAsDictionary<T>() where T : struct
        {
            var enumType = typeof(T);

            if (!enumType.IsEnum)
                throw new ArgumentException("Not an enum.");

            var enumDictionary = new Dictionary<int, string>();

            foreach (int value in Enum.GetValues(enumType))
                enumDictionary.Add(value, Enum.GetName(enumType, value));

            return enumDictionary;
        }

        public static void SetSelectedIndex(this DropDownList control, Predicate<ListItem> match)
        {
            for (int i = 0; i < control.Items.Count; i++)
            {
                if (match(control.Items[i]))
                {
                    control.SelectedIndex = i;
                }
            }
        }


        public static SPFeature ActivateFeature(this SPSite site, Guid featureId, SPFeaturePropertyCollection properties, SPFeatureDefinitionScope featureDefinitionScope)
        {
            return ActivateFeature(site, featureId, null, properties, false, false, featureDefinitionScope);
        }

        public static SPFeature ActivateFeature(this SPSite site, Guid featureId, SPFeaturePropertyCollection properties, bool force, SPFeatureDefinitionScope featureDefinitionScope)
        {
            return ActivateFeature(site, featureId, null, properties, force, false, featureDefinitionScope);
        }

        public static SPFeature ActivateFeature(this SPSite site, Guid featureId, Version version, SPFeaturePropertyCollection properties, bool force, bool fMarkOnly, SPFeatureDefinitionScope featureDefinitionScope)
        {
            Type type = typeof(SPFeatureCollection);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = type.GetMethod("AddInternal",
                flags,
                null,
                new Type[] { typeof(Guid), 
                    typeof(Version), 
                    typeof(SPFeaturePropertyCollection), 
                    typeof(bool), 
                    typeof(bool), 
                    typeof(SPFeatureDefinitionScope) },
               null);
            object obj = null;

            try
            {
                obj = method.Invoke(site.Features, new object[] { featureId, version, properties, force, fMarkOnly, featureDefinitionScope });
            }
            catch (TargetInvocationException invocationException)
            {
                throw invocationException.InnerException;
            }

            return (SPFeature)obj;
        }

        public static SPFeaturePropertyCollection BuildFeatureCollectionFromXml(SPFeature feature, string xml)
        {
            SPFeaturePropertyCollection collection = GetFeaturePropertyCollection(feature);
            Type type = typeof(SPFeaturePropertyCollection);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = type.GetMethod("BuildPropertyCollectionFromXml", flags, null, new Type[] { typeof(string) }, null);

            try
            {
                method.Invoke(collection, new object[] { xml });
            }
            catch (TargetInvocationException invocationException)
            {
                throw invocationException.InnerException;
            }

            return collection;
        }

        public static SPFeaturePropertyCollection BuildFeatureCollectionFromXmlNode(SPFeature feature, XmlNode node)
        {
            SPFeaturePropertyCollection collection = GetFeaturePropertyCollection(feature);
            Type type = typeof(SPFeaturePropertyCollection);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = type.GetMethod("BuildPropertyCollectionFromXmlNode", flags, null, new Type[] { typeof(XmlNode) }, null);

            try
            {
                method.Invoke(collection, new object[] { node });
            }
            catch (TargetInvocationException invocationException)
            {
                throw invocationException.InnerException;
            }

            return collection;
        }

        public static SPFeaturePropertyCollection BuildFeatureCollectionFromDictionary(SPFeature feature, IDictionary<string, string> properties)
        {
            SPFeaturePropertyCollection collection = GetFeaturePropertyCollection(feature);

            foreach (string key in properties.Keys)
                collection.Add(GetFeatureProperty(collection, key, properties[key]));

            return collection;
        }

        public static SPFeaturePropertyCollection GetFeaturePropertyCollection(SPFeature feature)
        {
            Type type = typeof(SPFeaturePropertyCollection);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            ConstructorInfo constructor = type.GetConstructor(flags,
                null,
                new Type[] { typeof(SPFeature) },
                null);
            object obj = null;

            try
            {
                obj = constructor.Invoke(new object[] { feature });
            }
            catch (TargetInvocationException invocationException)
            {
                throw invocationException.InnerException;
            }

            return (SPFeaturePropertyCollection)obj;
        }

        public static SPFeatureProperty GetFeatureProperty(SPFeaturePropertyCollection featureCollection, string key, string value)
        {
            Type type = typeof(SPFeatureProperty);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            ConstructorInfo constructor = type.GetConstructor(flags,
                null,
                new Type[] { typeof(SPFeaturePropertyCollection), typeof(string), typeof(string) },
                null);
            object obj = null;

            try
            {
                obj = constructor.Invoke(new object[] { featureCollection, key, value });
            }
            catch (TargetInvocationException invocationException)
            {
                throw invocationException.InnerException;
            }

            return (SPFeatureProperty)obj;
        }

        public static Term CreateTerm(SPSite site, string groupName, string termSetName, string parentTermName, string termName)
        {
            Term newTerm = null;
            SPSecurity.RunWithElevatedPrivileges(() =>
            {
                using (var site2 = new SPSite(site.ID, site.Zone))
                {
                    TaxonomySession session = new TaxonomySession(site2);
                    var termStore = session.TermStores[0];
                    var group = (from g in termStore.Groups
                                 where g.Name == groupName
                                 select g
                                ).FirstOrDefault();

                    var termSet = group.TermSets[termSetName];
                    
                    if (termSet != null)
                    {
                        var term = (from t in termSet.GetAllTerms()
                                   where t.Name == termName
                                   select t).FirstOrDefault();

                        if (term == null)
                        {
                            var parentTerm = (from t in termSet.GetAllTerms()
                                              where t.Name == parentTermName
                                              select t).FirstOrDefault();
                            if (parentTerm != null)
                            {
                                newTerm = parentTerm.CreateTerm(termName, site.RootWeb.Locale.LCID);
                                termStore.CommitAll();
                            }
                        }
                        else
                            newTerm = term;
                    }
                }
            });
            return newTerm;
        }

    }
}
