using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Collections;
using Microsoft.Office.Server.Audience;

namespace AEC.EnergyPortal.Core
{
    public class SecurityHelper
    {
        private enum AudienceAuthMode { audience, siteGroup };

        /// <summary>
        /// Checks site group membership in the Current Web
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="siteGroupName"></param>
        /// <returns></returns>
        public static bool IsGroupMember(SPUser currentUser, string siteGroupName)
        {
            bool isMember = false;

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                if (SPContext.Current != null)
                {
                    SPWeb web = SPContext.Current.Web;
                    try
                    {
                        foreach (SPGroup g in currentUser.Groups)
                        {
                            isMember = g.Name.Equals(siteGroupName);
                            if (isMember)
                                break;
                        }
                    }
                    catch (SPException x)
                    {
                        //Logger.Error("Site Group not found.", x);
                    }
                    catch (Exception x)
                    {
                        //Logger.Error("Unable to find the Site Group name: " + siteGroupName, x);
                        throw new Exception("Unable to find the Site Group name: " + siteGroupName, x);
                    }
                }
                else
                {
                    throw new Exception("Cannot determine whether currently logged-in user belongs to the Site Group '{0}' within the Current Http Context.");
                }
            });

            return isMember;
        }

        /// <summary>
        /// Determine whether the currently logged-in user is a member of the "siteGroupName" parameter
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="siteGroupName"></param>
        /// <returns></returns>
        public static bool IsGroupMember(string siteGroupName)
        {
            bool isMember = false;

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                if (SPContext.Current != null)
                {
                    try
                    {
                        foreach (SPGroup g in SPContext.Current.Web.CurrentUser.Groups)
                        {
                            isMember = g.Name.Equals(siteGroupName);
                            if (isMember)
                                break;
                        }
                    }
                    catch (SPException x)
                    {
                        //Logger.Error("Site Group not found.", x);
                    }
                    catch (Exception x)
                    {
                        //Logger.Error("Unable to find the Site Group name: " + siteGroupName, x);
                        throw new Exception("Unable to find the Site Group name: " + siteGroupName, x);
                    }
                }
                else
                {
                    throw new Exception("Cannot determine whether currently logged-in user belongs to the Site Group '{0}' within the Current Http Context.");
                }
            });

            return isMember;
        }

        public static bool IsGroupMember(SPUser currentUser, string siteGroupName, string requestedSite)
        {
            bool isMember = false;

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite site = new SPSite(requestedSite))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        try
                        {
                            foreach (SPGroup g in currentUser.Groups)
                            {
                                isMember = g.Name.Equals(siteGroupName);
                                if (isMember)
                                    break;
                            }
                        }
                        catch (SPException x)
                        {
                            //Logger.Error("Site Group not found.", x);
                        }
                        catch (Exception x)
                        {
                            //Logger.Error("Unable to find the Site Group name: " + siteGroupName, x);
                            throw new Exception("Unable to find the Site Group name: " + siteGroupName, x);
                        }
                    }
                }
            });

            return isMember;
        }

        /// <summary>
        /// Determines whether the user belongs to any of the site group names in the siteGroupNames List.
        /// </summary>
        /// <param name="siteGroupNames"></param>
        /// <param name="requestedSite"></param>
        /// <returns></returns>
        public static bool IsGroupMember(SPUser currentUser, List<string> siteGroupNames, string requestedSite)
        {
            bool isMember = false;
            bool validSite = true;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                try
                {
                    SPSite site = new SPSite(requestedSite);
                }
                catch { validSite = false; }
            });

            if (!validSite)
                return false;

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite site = new SPSite(requestedSite))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        try
                        {
                            foreach (SPGroup g in currentUser.Groups)
                            {
                                isMember = siteGroupNames.Contains(g.Name);
                                if (isMember)
                                    break;
                            }
                        }
                        catch (SPException x)
                        {
                            //Logger.Error("Site Group not found.", x);
                        }
                        catch (Exception x)
                        {
                            //Logger.Error("Unable to find the Site Group name.", x);
                            throw new Exception("Unable to find the Site Group name.", x);
                        }
                    }
                }
            });

            return isMember;
        }

        public static bool IsAudienceMember(SPUser currentUser, string audienceName)
        {
            if (string.IsNullOrEmpty(audienceName)) return false;
            bool retVal = false;

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite site = new SPSite(SPContext.Current.Site.ID))
                {
                    SPServiceContext svcContext = SPServiceContext.GetContext(site);
                    AudienceManager audMgr = null;
                    try { audMgr = new AudienceManager(svcContext); }
                    catch (Exception x)
                    {
                        //Logger.Error("AudienceManager error: The User Profile Service App is not running under the site: '" + site.RootWeb.Title + "'.", x);
                    }
                    if (audMgr != null)
                    {
                        using (SPWeb web = site.AllWebs[0])
                        {
                            ArrayList audIds = audMgr.GetUserAudienceIDs(currentUser.LoginName, true, web);
                            ArrayList audNames = new ArrayList();

                            for (int i = 0; i < audIds.Count; i++)
                            {
                                AudienceNameID n = (AudienceNameID)audIds[i];
                                if (n.AudienceName.Equals(audienceName))
                                {
                                    retVal = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            });

            return retVal;
        }
    }
}
