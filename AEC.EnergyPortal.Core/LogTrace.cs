using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace AEC.EnergyPortal.Core
{
    public class LogTrace
    {
        public enum EntryType { Error, ErrorCritical, Information, Warning, Verbose, None };

        public static void WriteUlsEntry(string usrMsg, EntryType entryType)
        {
            WriteUlsEntry(usrMsg, entryType, null);
        }

        /// <summary>
        /// Writes an entry to the ULS...
        /// </summary>
        /// <param name="usrMsg">User defined message.</param>
        /// <param name="ex">Thrown exception.</param>
        public static void WriteUlsEntry(string usrMsg, EntryType entryType, Exception x)
        {
            StringBuilder msg = new StringBuilder();
            if (x == null)
                msg.Append(usrMsg);
            else
                msg.AppendFormat("USER MESSAGE: {0}; EXCEPTION: {1}", usrMsg, x.Message);

            EventSeverity eventSev;
            TraceSeverity traceSev;
            switch (entryType)
            {
                case EntryType.Error:
                    eventSev = EventSeverity.Error;
                    traceSev = TraceSeverity.High;
                    break;
                case EntryType.ErrorCritical:
                    eventSev = EventSeverity.ErrorCritical;
                    traceSev = TraceSeverity.Unexpected;
                    break;
                case EntryType.Information:
                    eventSev = EventSeverity.Information;
                    traceSev = TraceSeverity.Medium;
                    break;
                case EntryType.Warning:
                    eventSev = EventSeverity.Warning;
                    traceSev = TraceSeverity.Medium;
                    break;
                case EntryType.Verbose:
                    eventSev = EventSeverity.Verbose;
                    traceSev = TraceSeverity.Medium;
                    break;
                default:
                    eventSev = EventSeverity.None;
                    traceSev = TraceSeverity.Medium;
                    break;
            }

            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                SPDiagnosticsService.Local.WriteTrace(0,
                    new SPDiagnosticsCategory("AEC.EnergyPortal", traceSev, eventSev),
                    traceSev,
                    msg.ToString(),
                    (x!=null)? x.StackTrace : null);
            });
        }
    }
}
