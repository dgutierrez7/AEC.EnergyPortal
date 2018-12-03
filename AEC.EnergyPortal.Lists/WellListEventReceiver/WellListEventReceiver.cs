using System;
using System.Collections;
using System.Linq;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.BusinessData.MetadataModel;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.Workflow;
using AEC.EnergyPortal.Core;
using System.Configuration;

namespace AEC.EnergyPortal.Lists.WellListEventReceiver
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class WellListEventReceiver : SPItemEventReceiver
    {
        private static readonly string WELL_IDENTIFIER_FIELD_DISPLAY_NAME = "Epex Well ID";
        // the field in master list that uniquely identifies the well

        private static readonly string STATUS_FIELD_DISPLAY_NAME = "Project Status";
        // the field in properties list that will indicate a site was deleted

        private static readonly string STATUS_FIELD_DELETED_VALUE = "Deleted";
            // value for the status field to indicate a site was deleted

        private ConfigSettings configSettings;
        private bool _verboseLogging;

        /// <summary>
        /// An item was added to the master list.
        /// </summary>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            if(_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: MasterWellList ItemAdded", LogTrace.EntryType.Information);
            DateTime entryTime = DateTime.Now;
            base.ItemAdded(properties);

            initialize(properties.Web); //This function supplied by LV and present in the event receiver stubs. Currently not used in the event receivers.
  
            string wellIdentifier = GetWellIdentifier(properties.ListItem); // Get the unique well ID

            if (String.IsNullOrEmpty(wellIdentifier)) // well identifier should never be blank
                LogTrace.WriteUlsEntry("[MWL] Cannot create new well site - well identifier is blank", LogTrace.EntryType.Error);
            else
                ProvisionWellSite(wellIdentifier, properties); // Provision new well site:

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG-End-ItemAdded]";
                double deltaSec = delta.TotalSeconds;
                LogTrace.WriteUlsEntry(lblDiag + " Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }
        }

        /// <summary>
        /// An item was updated in the master list.
        /// </summary>
        public override void ItemUpdated(SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG-Start-ItemUpdated] Enter scope: ItemUpdated; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            base.ItemUpdated(properties);
            initialize(properties.Web); //This function supplied by LV and present in the event receiver stubs. Currently not used in the event receivers.

            int changedPropsCount = 0;
            string epexItem = properties.ListItem[WELL_IDENTIFIER_FIELD_DISPLAY_NAME].ToString().Trim();
            if (_verboseLogging) LogTrace.WriteUlsEntry("[MWL-UPDATEDFIELDS-START] Updated EPEX ID: " + epexItem, LogTrace.EntryType.Information);
            foreach (DictionaryEntry changeField in properties.AfterProperties) // A list of the fields with new values
            {
                string fieldName = changeField.Key.ToString(); // Internal name
                string fieldValue = changeField.Value.ToString();
                if (_verboseLogging) LogTrace.WriteUlsEntry("[MWL-UPDATEDFIELDS-AfterProperties] Field: " + fieldName + "Field Value: " + fieldValue, LogTrace.EntryType.Information);
                changedPropsCount++;
            }
            if (_verboseLogging) LogTrace.WriteUlsEntry("[MWL-UPDATEDFIELDS-COUNT]: " + changedPropsCount, LogTrace.EntryType.Information); 

            string wellIdentifier = GetWellIdentifier(properties.ListItem); // Get the unique well ID (EPEX ID)

            if (String.IsNullOrEmpty(wellIdentifier)) // well identifier should never be blank
                LogTrace.WriteUlsEntry("[MWL] Cannot update existing well site - well identifier is blank", LogTrace.EntryType.Error);
            else
            {
                if(changedPropsCount > 0)
                    UpdateWellSite(wellIdentifier, properties); // Update existing well site
            }

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG-End-ItemUpdated]";
                double deltaSec = delta.TotalSeconds;
                LogTrace.WriteUlsEntry(lblDiag + " Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }
        }

        /// <summary>
        /// An item iss deleted.
        /// </summary>
        public override void ItemDeleting(SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG-Start-ItemDeleting] Enter scope: ItemDeleting; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            base.ItemDeleting(properties);

            initialize(properties.Web); //This function supplied by LV and present in the event receiver stubs. Currently not used in the event receivers.

            string wellIdentifier = GetWellIdentifier(properties.ListItem); // Get the unique well ID

            if (String.IsNullOrEmpty(wellIdentifier)) // well identifier should never be blank
                LogTrace.WriteUlsEntry("[MWL] Cannot flag existing well site as deleted - well identifier is blank", LogTrace.EntryType.Error);
            else
                DeleteWellSite(wellIdentifier, STATUS_FIELD_DISPLAY_NAME, STATUS_FIELD_DELETED_VALUE, properties); // Mark existing well site as deleted

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG-End-ItemDeleting]";
                double deltaSec = delta.TotalSeconds;
                LogTrace.WriteUlsEntry(lblDiag + " Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }
        }

        //****** Need info on purpose of this call ****************
        // It was in the solution given to us by LV but it is not needed by our code.
        private void initialize(SPWeb w)
        {
            try
            {
                configSettings = new ConfigSettings(w);
                _verboseLogging = Convert.ToBoolean(ConfigurationManager.AppSettings["DisplayVerboseLogging"]);
            }
            catch (Exception ex)
            {
                LogTrace.WriteUlsEntry(String.Format("[MWL] Cannot load ConfigSettings. Reason:{0}", ex), LogTrace.EntryType.Warning);
            }
        }

        /// <summary>
        /// Provision a new well site in Well Central
        /// wellIdentifier - identifies the well site in Well Central
        /// </summary>
        private void ProvisionWellSite(string wellIdentifier, SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: ProvisionWellSite; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            // See if site already exists in well central
            string siteCollectionUrl = properties.Site.Url; // the same site collection as this list 
            string url = GetWellSiteUrl(siteCollectionUrl, wellIdentifier);
            if (url != String.Empty)
            {
                LogTrace.WriteUlsEntry(
                    string.Format("[MWL] Provision well site. No action taken. Well {0} already existed at {1}", wellIdentifier, url),
                    LogTrace.EntryType.Warning);
            }
            else
            {
                try
                {
                    url = CreateWellProperties(wellIdentifier, properties);
                    if (url != string.Empty)
                        LogTrace.WriteUlsEntry(string.Format("[MWL] Provision well site. Well {0} created at {1}", wellIdentifier, url), LogTrace.EntryType.Information);
                }
                catch (Exception ex)
                {
                    LogTrace.WriteUlsEntry(
                        string.Format("[MWL] Provision well site: Failed to create Well {0}. Reason: {1}", wellIdentifier, ex),
                        LogTrace.EntryType.Error);
                }
            }

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG]";
                double deltaSec = delta.TotalSeconds;
                if (deltaSec > 1)
                    lblDiag = "[DIAG-FLAGGED]";
                LogTrace.WriteUlsEntry(lblDiag + " Exit scope: ProvisionWellSite; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }

        }

        /// <summary>
        /// Update existing well site in Well Central
        /// wellIdentifier - identifies the well site in Well Central
        /// </summary>
        private void UpdateWellSite(string wellIdentifier, SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: UpdateWellSite; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            // Locate well site in current site collection
            string siteCollectionUrl = properties.Site.Url; 

            string url = GetWellSiteUrl(siteCollectionUrl, wellIdentifier); // Get the FQDN of the well site or blank if cannot locate based on wellIdentifier
            if (String.IsNullOrEmpty(url)) // Failed to locate well based on unique well id
            {
                LogTrace.WriteUlsEntry(
                    string.Format("[MWL] Abort update well site. No action taken. Well {0} not found", wellIdentifier),
                    LogTrace.EntryType.Warning);
            }
            else // we have a url for the well site
            {
                // Update the Well Site Link field, if null...
                try
                {
                    /// [R.Lucier] - Removing the test for null. Update the Well Site Link field regardless of whether the value is null.
                    ///
                    if (properties.AfterProperties["Title"] != null)
                    {
                        if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Updating 'Well Site Link' field.", LogTrace.EntryType.Information);
                        SPFieldUrlValue value = new SPFieldUrlValue();
                        value.Description = Convert.ToString(properties.ListItem["Title"]);
                        value.Url = url;
                        properties.ListItem["Well Site Link"] = value;
                        if (_verboseLogging) LogTrace.WriteUlsEntry(String.Format("[MWL] value.Url: {0}", value.Url), LogTrace.EntryType.Information);
                        properties.ListItem.Update();
                    }
                }
                catch { LogTrace.WriteUlsEntry(String.Format("[MWL] Unable to set the 'Well Site Link' field within the ItemUpdated scope. Url: {0}.", url), LogTrace.EntryType.Error); }
                try
                {
                    UpdateWellProperties(url, properties); // Update its properties
                    LogTrace.WriteUlsEntry(string.Format("[MWL] Update well site. Well {0} at {1} updated", wellIdentifier, url),
                            LogTrace.EntryType.Information);
                }
                catch (Exception ex)
                {
                    LogTrace.WriteUlsEntry(
                        string.Format("[MWL] Update well site: Failed to update Well {0} at {1}. Reason: {2}", wellIdentifier, url, ex),
                        LogTrace.EntryType.Error);
                }
            }

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG]";
                double deltaSec = delta.TotalSeconds;
                if (deltaSec > 1)
                    lblDiag = "[DIAG-FLAGGED]";
                LogTrace.WriteUlsEntry(lblDiag + " Exit scope: UpdateWellSite; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }

        }

        /// <summary>
        /// Mark a well site in Well Central with a "deleted" flag
        /// wellIdentifier - identifies the well site in Well Central
        /// The statusFieldName = the display name of the status field to hold the value
        /// The statusFieldValue = the text to place in the status field indicating deleted
        /// </summary>
        private void DeleteWellSite(string wellIdentifier, string statusFieldName,
            string statusFieldValue, SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: DeleteWellSite; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            string siteCollectionUrl = properties.Site.Url;
            string url = GetWellSiteUrl(siteCollectionUrl, wellIdentifier); // Get the FQDN of the well site or blank if cannot locate based on wellIdentifier
            if (String.IsNullOrEmpty(url)) // Failed to locate well based on unique well id
            {
                LogTrace.WriteUlsEntry(
                    string.Format("[MWL] Abort delete well site: No action taken. Well {0} not found", wellIdentifier),
                    LogTrace.EntryType.Warning);
            }
            else
            {
                try
                {
                    UpdateWellPropertyStatus(url, statusFieldName, statusFieldValue); // Update the status field with the delete value
                    LogTrace.WriteUlsEntry(string.Format("[MWL] Delete well site: Well {0} at {1} flagged as deleted", wellIdentifier, url), LogTrace.EntryType.Information);
                }
                catch (Exception ex)
                {
                    LogTrace.WriteUlsEntry(
                        string.Format("[MWL] Delete well site: Failed to flag Well {0} at {1} as deleted. Reason: {2}",
                            wellIdentifier, url, ex), LogTrace.EntryType.Error);
                }
            }

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG]";
                double deltaSec = delta.TotalSeconds;
                if (deltaSec > 1)
                    lblDiag = "[DIAG-FLAGGED]";
                LogTrace.WriteUlsEntry(lblDiag + " Exit scope: DeleteWellSite; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }
        }

        /// <summary>
        /// Get the unique well id from a listy item.
        /// return blank if none found
        /// We use display name as P4E may have an internal name that cannot be reused by the master list 
        /// </summary>
        private string GetWellIdentifier(SPListItem masterItem)
        {
            DateTime entryTime = DateTime.Now;
            //LogTrace.WriteUlsEntry("[DIAG] Enter scope: GetWellIdentifier; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);
            string wellIdentifier = string.Empty;
            if (!masterItem.Fields.ContainsField(WELL_IDENTIFIER_FIELD_DISPLAY_NAME)) // The master list must have a field with the right display name
            {
                LogTrace.WriteUlsEntry(string.Format("[MWL] Well Identifier parsing failed: field {0} does not exist in list {1}", WELL_IDENTIFIER_FIELD_DISPLAY_NAME, masterItem.ParentList.Title), LogTrace.EntryType.Error);
            }
            else
            {
                if (masterItem[WELL_IDENTIFIER_FIELD_DISPLAY_NAME] == null)
                    LogTrace.WriteUlsEntry(
                        string.Format("[MWL] Well Identifier parsing failed: the well identifier value is unassigned"),
                        LogTrace.EntryType.Error);
                else
                {
                    wellIdentifier = masterItem[WELL_IDENTIFIER_FIELD_DISPLAY_NAME].ToString().Trim(); 
                    if (String.IsNullOrEmpty(wellIdentifier)) // Should never be blank
                        LogTrace.WriteUlsEntry(
                            string.Format("[MWL] Well Identifier parsing failed: the well identifier value is blank"),
                            LogTrace.EntryType.Error);
                }
            }

            DateTime exitTime = DateTime.Now;
            TimeSpan delta = exitTime.Subtract(entryTime);
            string lblDiag = "[DIAG]";
            double deltaSec = delta.TotalSeconds;
            if (deltaSec > 1)
                lblDiag = "[DIAG-FLAGGED]";
            LogTrace.WriteUlsEntry(lblDiag + " Exit scope: GetWellIdentifier; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            return wellIdentifier;
        }

        /// <summary>
        /// Locate a well site based on the supplied well identifier
        /// return the well site's url or String.Empty if not found
        /// </summary>
        private string GetWellSiteUrl(string siteCollectionUrl, string wellIdentifier)
        {
            DateTime entryTime = DateTime.Now;
            //LogTrace.WriteUlsEntry("[DIAG] Enter scope: GetWellSiteUrl; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);
            string url = String.Empty; 
            SPSecurity.RunWithElevatedPrivileges(delegate() // We are looking at the projects cache for performance - may need to be elevated
            {
                using (SPSite site = new SPSite(siteCollectionUrl))
                {
                    using (SPWeb web = site.RootWeb)
                    {
                        SPList list = web.Lists["ProjectsCache"]; // Access the hidden ProjectsCache 
                        SPQuery query = new SPQuery(); // Look for a site where the cached name = wellIdentifier
                        query.Query = string.Format("<Where><Eq><FieldRef Name='EpexID' /><Value Type='Text'>{0}</Value></Eq></Where>", wellIdentifier);
                        SPListItemCollection items = list.GetItems(query);

                        if (items.Count == 0) // no site found
                            LogTrace.WriteUlsEntry(string.Format("[MWL] Well Identifier {0} not found in ProjectsCache.ProjectNameCached", wellIdentifier), LogTrace.EntryType.Error);
                        else if (items.Count > 1) // found multiple matching vcalues - should never happen
                            LogTrace.WriteUlsEntry(string.Format("[MWL] Well Identifier {0} found multiple times in ProjectsCache.ProjectNameCached", wellIdentifier), LogTrace.EntryType.Error);
                        else // found exactly one candidate
                        {
                            url = String.Format("{0}/{1}", siteCollectionUrl, items[0]["ProjectUrlCached"]); // the FQDN
                        }
                    }
                }
            });

            DateTime exitTime = DateTime.Now;
            TimeSpan delta = exitTime.Subtract(entryTime);
            string lblDiag = "[DIAG]";
            double deltaSec = delta.TotalSeconds;
            if (deltaSec > 1)
                lblDiag = "[DIAG-FLAGGED]";
            LogTrace.WriteUlsEntry(lblDiag + " Exit scope: GetWellSiteUrl; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);

            return url;
        }

        /// <summary>
        /// Check a field's attributes and see if it falls within the rules for copying.
        /// </summary>
        private bool FieldValueCanBeCopied(SPField field)
        {
            
            if (((field.ReadOnlyField) || 
                (field.FromBaseType) || 
                (field.Hidden) || 
                (field.InternalName == "Attachments") ||
                (field.Type == SPFieldType.Calculated) ||
                (field.Type == SPFieldType.Invalid) ||
                (field.Type == SPFieldType.ContentTypeId) || // these types are never copied - they can cause bad things to happen.
                (field.Title == WELL_IDENTIFIER_FIELD_DISPLAY_NAME))) // Dont copy this field - it should not be changing ever
                return false;
            else
                return true;
        }

        /// <summary>
        /// Copy certain data fields from source to dest
        /// The return value = true if a change occurred otherwise false
        /// We use display names. Internal field names between source and dest may differ or P4E lists are using/reserving internal names 
        /// </summary>
        private bool CopyListItemContent(SPItemEventProperties properties, SPListItem dest)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: CopyListItemContent; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);
            bool dataChanged = false;

            foreach(DictionaryEntry changeField in properties.AfterProperties) // A list of the fields with new values
            {
                string fieldName = changeField.Key.ToString(); // Internal name
                try
                {
                    string sourceFieldDisplayName = properties.ListItem.Fields.GetFieldByInternalName(fieldName).Title; // the display name
                    if (dest.Fields.ContainsField(sourceFieldDisplayName)) // Compare display names not internal names
                    {
                        if (FieldValueCanBeCopied(dest.Fields[sourceFieldDisplayName])) // Dont copy certain kinds of fields that can be problematic
                        {
                            dest[sourceFieldDisplayName] = properties.AfterProperties[fieldName];
                            dataChanged = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogTrace.WriteUlsEntry(string.Format("[MWL] Unable to copy field named: {0} in source list to target properties list. Reason:{1}", fieldName, ex), LogTrace.EntryType.Error);
                }
            }

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG]";
                double deltaSec = delta.TotalSeconds;
                if (deltaSec > 1)
                    lblDiag = "[DIAG-FLAGGED]";
                LogTrace.WriteUlsEntry(lblDiag + " Exit scope: CopyListItemContent; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }

            return dataChanged;
        }

        /// <summary>
        /// Open well site at supplied url and update its property list
        /// </summary>
        private void UpdateWellProperties(string url, SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: UpdateWellProperties; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                LogTrace.WriteUlsEntry(String.Format("[MWL] Updating site at url {0}", url), LogTrace.EntryType.Information);
                using (SPSite site = new SPSite(url))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        try
                        {
                            SPList propertiesList = web.Lists["Properties"];
                            SPListItem propertiesListItem = propertiesList.Items[0];
                            if (CopyListItemContent(properties, propertiesListItem)) // Copy contents (from source -> dest) returns true if at least one field changed
                            {
                                bool allowUpdateSetting = web.AllowUnsafeUpdates;
                                web.AllowUnsafeUpdates = true;
                                propertiesListItem.Update(); 
                                web.AllowUnsafeUpdates = allowUpdateSetting;
                                LogTrace.WriteUlsEntry(String.Format("[MWL] Updated changed fields from MasterList to well site properties."), LogTrace.EntryType.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogTrace.WriteUlsEntry(String.Format("[MWL] Failed to copy fields from MasterList to well site properties. Reason: {0}", ex), LogTrace.EntryType.Error);
                        }
                    }
                }
            });

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG]";
                double deltaSec = delta.TotalSeconds;
                if (deltaSec > 1)
                    lblDiag = "[DIAG-FLAGGED]";
                LogTrace.WriteUlsEntry(lblDiag + " Exit scope: UpdateWellProperties; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }
        }

        /// <summary>
        /// We will check <iteration> times with <seconds> delay between each to see if the site was created by ProjectsQueue code.
        /// This function is generally used if you need to get a url back for other uses or
        ///   you want to log that the site was created and it's url or
        ///   if you want to log that no site was created in a reasonable time frame.
        // It can safely be bypassed if none of these are of interest. 
        /// </summary>
        private string WaitForSiteCreation(string siteCollectionUrl, SPListItem newSiteEntry, int seconds, int iterations)
        {
            DateTime entryTime = DateTime.Now;
            //LogTrace.WriteUlsEntry("[DIAG] Enter scope: WaitForSiteCreation; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            string siteUrl = String.Empty;
            do
            {
                if (newSiteEntry != null)
                {
                    if (!String.IsNullOrEmpty(newSiteEntry["ProjectQueueSiteUrl"] as String))
                    {
                        siteUrl = String.Format("{0}/{1}", siteCollectionUrl,
                            newSiteEntry["ProjectQueueSiteUrl"] as String);
                        iterations = 0;
                    }
                    else
                        System.Threading.Thread.Sleep(seconds * 1000);
                }
            } while (iterations-- > 0);

            DateTime exitTime = DateTime.Now;
            TimeSpan delta = exitTime.Subtract(entryTime);
            string lblDiag = "[DIAG]";
            double deltaSec = delta.TotalSeconds;
            if (deltaSec > 1)
                lblDiag = "[DIAG-FLAGGED]";
            LogTrace.WriteUlsEntry(lblDiag + " Exit scope: WaitForSiteCreation; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);

            return siteUrl;
        }

        /// <summary>
        /// Create site based on supplied well identifier in current site collection
        /// </summary>
        private string CreateWellProperties(string wellIdentifier, SPItemEventProperties properties)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: CreateWellProperties; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            string siteUrl = string.Empty; // this will hold the url of the newly created site
            string siteCollectionUrl = properties.Site.Url; // create a project in the same site collection as this list 

            if (String.IsNullOrEmpty(wellIdentifier)) // Should never happen
                LogTrace.WriteUlsEntry("[MWL] Cannot create new well site - well identifier is blank", LogTrace.EntryType.Error);
            else
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    LogTrace.WriteUlsEntry(String.Format("[MWL] Processing new site creation in {0}", siteCollectionUrl), LogTrace.EntryType.Information);
                    using (SPSite site = new SPSite(siteCollectionUrl))
                    {
                        using (SPWeb web = site.RootWeb)
                        {
                            string templateName = site.RootWeb.Properties["projectsqueue_templateurl"]; // Property bag entry holds the name of the project template.
                            if (String.IsNullOrEmpty(templateName)) // Should never happen - configuration error
                            {
                                LogTrace.WriteUlsEntry("[MWL] No template entry found in root site property bag 'projectsqueue_templateurl'", LogTrace.EntryType.ErrorCritical);
                            }
                            else
                            {
                                LogTrace.WriteUlsEntry(String.Format("[MWL] Found template name entry in root site property bag 'projectsqueue_templateurl': {0}", templateName), LogTrace.EntryType.Information);
                                using (SPWeb template = site.OpenWeb(templateName)) // Open the template
                                {
                                    SPList propertyList = template.Lists.TryGetList("Properties"); // Look for a properties list
                                    if (propertyList == null) // If it isnt there we cannot proceed - likely configuration error or corrupt template
                                    {
                                        LogTrace.WriteUlsEntry("[MWL] Could not find properties list in template", LogTrace.EntryType.ErrorCritical);
                                    }
                                    else
                                    {
                                        LogTrace.WriteUlsEntry(String.Format("[MWL] Scheduling site creation for project name: {0}", wellIdentifier), LogTrace.EntryType.Information);
                                        try
                                        {
                                            SPList projectsQueue = web.Lists["ProjectsQueue"]; // Create a new entry in the projectsqueue list
                                            if (!projectsQueue.Fields.ContainsField(WELL_IDENTIFIER_FIELD_DISPLAY_NAME))
                                            {
                                                LogTrace.WriteUlsEntry(String.Format("[MWL] Could not find field {0} in properties list in template", WELL_IDENTIFIER_FIELD_DISPLAY_NAME), LogTrace.EntryType.ErrorCritical);
                                            }
                                            else
                                            {
                                                SPListItem newItem = projectsQueue.AddItem();

                                                // Populate the entry with minimum fields needed to create a site
                                                newItem[WELL_IDENTIFIER_FIELD_DISPLAY_NAME] = wellIdentifier;
                                                newItem["Title"] = wellIdentifier;

                                                CopyListItemContent(properties, newItem); // Copy contents (from source -> dest)

                                                newItem["ProjectQueueStatus"] = "Ready"; // this flag signals the project queue manager that this item is a valid site creator record
                                               
                                                bool allowUpdateSetting = web.AllowUnsafeUpdates;
                                                web.AllowUnsafeUpdates = true;
                                                newItem.Update(); // This causes the ProjectsQueue to initiate creation of a new site
                                                web.AllowUnsafeUpdates = allowUpdateSetting; 

                                                // Start of optional site creation check code that can be bypassed
                                                int secondsWait = 5;
                                                int iterations = 3;
                                                siteUrl = WaitForSiteCreation(siteCollectionUrl, newItem, secondsWait, iterations);
                                                if (siteUrl == String.Empty)
                                                {
                                                    LogTrace.WriteUlsEntry(String.Format("[MWL] Unable to confirm creation of new site named {0} after {1} seconds delay. Unable to set 'Well Site Link' field.", wellIdentifier, (secondsWait * iterations)), LogTrace.EntryType.Warning);
                                                }
                                                else
                                                {
                                                    try{
                                                        SPFieldUrlValue value = new SPFieldUrlValue();
                                                        value.Description = Convert.ToString(properties.ListItem["Title"]);
                                                        value.Url = siteUrl;
                                                        properties.ListItem["Well Site Link"] = value;
                                                        LogTrace.WriteUlsEntry(String.Format("[MWL] value.Url: {0}", value.Url), LogTrace.EntryType.Information);
                                                        properties.ListItem.Update();
                                                    }
                                                    catch { LogTrace.WriteUlsEntry(String.Format("[MWL] Unable to set the 'Well Site Link' field. siteUrl: {0}.", siteUrl), LogTrace.EntryType.Error); }
                                                    LogTrace.WriteUlsEntry(String.Format("[MWL] New well site created. URL: {0}.", siteUrl),LogTrace.EntryType.Information);
                                                }
                                                // End of site creation check code that can be bypassed

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LogTrace.WriteUlsEntry(String.Format("[MWL] Unable to schedule creation of new site named {0}. Reason: {1}", wellIdentifier, ex), LogTrace.EntryType.Error);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }); // run with elevated privileges

                if (String.IsNullOrEmpty(siteUrl))
                    LogTrace.WriteUlsEntry(String.Format("[MWL] After wait time, did not detect a new url for project name: {0}", wellIdentifier), LogTrace.EntryType.Warning);
                else
                    LogTrace.WriteUlsEntry(String.Format("[MWL] Detected a new a url {0} for project name: {1}", siteUrl, wellIdentifier), LogTrace.EntryType.Information);
                }

            if (_verboseLogging)
            {
                DateTime exitTime = DateTime.Now;
                TimeSpan delta = exitTime.Subtract(entryTime);
                string lblDiag = "[DIAG]";
                double deltaSec = delta.TotalSeconds;
                if (deltaSec > 1)
                    lblDiag = "[DIAG-FLAGGED]";
                LogTrace.WriteUlsEntry(lblDiag + " Exit scope: CreateWellProperties; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
            }

            return siteUrl;
        }

        /// <summary>
        /// Open well site at supplied url and update properties list, status field with status field value
        /// </summary>
        private void UpdateWellPropertyStatus(string url, string statusFieldName, string statusFieldValue)
        {
            DateTime entryTime = DateTime.Now;
            if (_verboseLogging) LogTrace.WriteUlsEntry("[DIAG] Enter scope: UpdateWellPropertyStatus; Entry time: " + entryTime.ToLongTimeString(), LogTrace.EntryType.Information);

            if (String.IsNullOrEmpty(url)) // Should never happen
                LogTrace.WriteUlsEntry("[MWL] Cannot flag well site as deleted - well url is blank", LogTrace.EntryType.Error);
            else
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    LogTrace.WriteUlsEntry(String.Format("[MWL] Processing site deletion flag for site in {0}", url),
                        LogTrace.EntryType.Information);
                    using (SPSite site = new SPSite(url))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            try
                            {
                                SPList propertiesList = web.Lists["Properties"];
                                SPListItem propertiesItem = propertiesList.Items[0];
                                if (!propertiesItem.Fields.ContainsField(statusFieldName))
                                {
                                    LogTrace.WriteUlsEntry(
                                        String.Format(
                                            "[MWL] Abort Updating status flag at well to indicate deleted. Field {0} did not exist in Properties list.",
                                            statusFieldName), LogTrace.EntryType.Error);
                                }
                                else
                                {
                                    propertiesItem[statusFieldName] = statusFieldValue;

                                    bool allowUpdateSetting = web.AllowUnsafeUpdates;
                                    web.AllowUnsafeUpdates = true;
                                    propertiesItem.Update();
                                    web.AllowUnsafeUpdates = allowUpdateSetting;

                                    LogTrace.WriteUlsEntry(
                                        String.Format("[MWL] Updated status flag at well to indicate deleted."),
                                        LogTrace.EntryType.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogTrace.WriteUlsEntry(
                                    String.Format("[MWL] Failed to update status flag indicating well deleted. Reason: {0}",
                                        ex), LogTrace.EntryType.Error);
                            }
                        }
                    }
                }); // run with elevated privileges

                if (_verboseLogging)
                {
                    DateTime exitTime = DateTime.Now;
                    TimeSpan delta = exitTime.Subtract(entryTime);
                    string lblDiag = "[DIAG]";
                    double deltaSec = delta.TotalSeconds;
                    if (deltaSec > 1)
                        lblDiag = "[DIAG-FLAGGED]";
                    LogTrace.WriteUlsEntry(lblDiag + " Exit scope: UpdateWellPropertyStatus; Exit  time: " + exitTime.ToLongTimeString() + " Delta: " + deltaSec, LogTrace.EntryType.Information);
                }

            }
        }
    }
}