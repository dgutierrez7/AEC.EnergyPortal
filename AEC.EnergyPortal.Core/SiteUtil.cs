using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint;
using System.Security.Permissions;

namespace AEC.EnergyPortal.Core
{
    public class SiteUtil
    {
        [SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
        public static void RemoveContentTypeFromWeb(SPWeb web, SPContentTypeId ctId)
        {
            var bestMatch = web.ContentTypes.BestMatch(ctId);

            if (bestMatch.IsChildOf(ctId))
            {
                if (bestMatch.Parent.Equals(ctId))
                {
                    // Is direct descendant
                    web.ContentTypes.Delete(bestMatch);
                }
            }
        }

        public static bool IsRootSite()
        {
            return SPContext.Current.Web.IsRootWeb;
        }
    }
}
