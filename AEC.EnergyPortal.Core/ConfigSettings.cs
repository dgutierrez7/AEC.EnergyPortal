using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace AEC.EnergyPortal.Core
{
    /// <summary>
    /// The name value pair for config entry
    /// </summary>
    public class ConfigEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class ConfigSettings
    {
        private SPWeb Web { get; set; }
        private SPList ConfigList { get; set; }

        /// <summary>
        /// The constructor to create the instance of config service
        /// </summary>
        /// <param name="web">The web that stores the config data</param>
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public ConfigSettings(SPWeb web)
        {
            this.Web = web;
            try
            {
                ConfigList = Web.GetList(ListUtil.GetListUrl(Web.ServerRelativeUrl, Globals.ListUrls.ConfigSettings));
            }
            catch
            {
                //Do Nothing
            }
        }

        /// <summary>
        /// This method returns all the config entries for a key
        /// </summary>
        /// <param name="key">The config key</param>
        /// <returns>The config entries for the key</returns>
        public SPListItemCollection GetItems(string key)
        {
            var query = new QueryBuilder().EqualFilter(Globals.Fields.ConfigKey, key).Build();
            SPListItemCollection items = null;
            items = ConfigList.GetItems(query);
            //ConfigList.ProcessItems(query,
            //    li =>
            //    {
            //        items = li;
            //    },
            //    (li, e) =>
            //    {
            //        return false;
            //    });
            return items;
        }

        /// <summary>
        /// This mentod returns the config entry for the specified key
        /// </summary>
        /// <param name="key">The config key</param>
        /// <returns>The config entry</returns>
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public ConfigEntry Read(string key)
        {
            if (ConfigList == null) 
                return null;
 
            ConfigEntry configEntry = null;
            var items = GetItems(key);
            
            if (items !=  null && items.Count > 0)
            {
                var item = items[0];
                configEntry = new ConfigEntry
                {
                    Key = item[Globals.Fields.ConfigKey].ToString(),
                    Value = item[Globals.Fields.ConfigValue].ToString()
                };
            }

            return configEntry;
        }

        /// <summary>
        /// This method writes the config entry to the data store
        /// </summary>
        /// <param name="entry">The config entry</param>
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public void Save(ConfigEntry entry)
        {
            if (ConfigList == null)
                return;

            var items = GetItems(entry.Key);

            if (items == null)
            {
                //Add Entry
                var list = ConfigList;
                var item = list.AddItem();
                item[Globals.Fields.ConfigKey] = entry.Key;
                item[Globals.Fields.ConfigValue] = entry.Value;
                item[Globals.Fields.Title] = entry.Key;
                item.Update();
            }
            else
            {
                //Update Entry
                var item = items[0];
                item[Globals.Fields.ConfigValue] = entry.Value;
                item.Update();
            }
        }

        /// <summary>
        /// This method deletes the config entry from the data store
        /// </summary>
        /// <param name="key">The config key</param>
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public void Delete(string key)
        {
            if (ConfigList == null)
                return;

            var items = GetItems(key);

            if (items != null)
            {
                var item = items[0];
                item.Delete();
            }
        }

        /// <summary>
        /// This method checks if config data store contains a config entry with the specified store.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public bool Contains(string key)
        {
            var items = GetItems(key);
            return (items != null && items.Count > 0);
        }
    }
}
