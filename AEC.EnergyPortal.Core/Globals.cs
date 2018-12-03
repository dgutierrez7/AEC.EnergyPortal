using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Drawing;
using System.Globalization;

namespace AEC.EnergyPortal.Core
{
    public static class Globals
    {
        public const string TermStore = "Managed Metadata Service";
        public const string Delimeter = ";#";

        #region Content Type ID's:
        public static readonly SPContentTypeId ItemContentTypeId = new SPContentTypeId("0x01");
        public static readonly SPContentTypeId DocumentContentTypeId = new SPContentTypeId("0x0101");
        public static readonly SPContentTypeId MasterpageContentTypeId = new SPContentTypeId("0x010105");

        //public static readonly string SchwabBasePageContentTypeId = "0x010100C568DB52D9D0A14D9B2FDCC96666E9F2007948130EC3DB064584E219954237AF3900A4A948CD091E49598A1E400A073C2877";

        #endregion

        #region Constants:
        public class TermGroups
        {
            public const string EnterpriseTaxonomy = "Enterprise Taxonomy";
        }

        public class TermSets
        {
            public const string EnterpriseTaxonomy = "Enterprise Taxonomy";
            public const string Region = "Region";
            public const string Template = "Template";
            public const string Offices = "Offices";
        }

        public static class FieldIds
        {
            // Sample:
            //public const string fieldName = "CF0CEF7B-1298-4928-9D3A-82AF07C0E9C8";
        }

        public static class Fields
        {
            public const string Title = "Title";
            public const string OperationStatus = "OperationStatus";
            public const string DrillingCost = "DrillingCost";
            public const string DrillingEstimate = "DrillingEstimate";
            public const string DrillingDays = "DrillingDays";
            public const string OBODrillingEstimate = "OBODrillingEstimate";
            public const string EstSpudDate = "EstSpudDate";
            public const string EstimatedSpud = "EstimatedSpud";
            public const string OBOEstimatedSpud = "OBOEstimatedSpud";
            public const string Rig = "Rig";
            public const string RigName = "RigName";
            public const string OBORigName = "OBORigName";
            public const string RigRelease = "RigRelease";
            public const string ActualRigRelease = "ActualRigRelease";
            public const string OBOActualRigRelease = "OBOActualRigRelease";
            public const string SpudDate = "SpudDate";
            public const string ActualSpud = "ActualSpud";
            public const string OBOActualSpud = "OBOActualSpud";
            public const string BHLatitude = "BHLatitude";
            public const string ActualBottomLatitude = "ActualBottomLatitude";
            public const string TargetBottomLatitude = "TargetBottomLatitude";
            public const string BHLongitude = "BHLongitude";
            public const string ActualBottomLongitude = "ActualBottomLongitude";
            public const string TargetBottomLongitude = "TargetBottomLongitude";
            public const string BHRange = "BHRange";
            public const string ActualBottomRange = "ActualBottomRange";
            public const string TargetBottomRange = "TargetBottomRange";
            public const string BHSection = "BHSection";
            public const string ActualBottomSection = "ActualBottomSection";
            public const string TargetBottomSection = "TargetBottomSection";
            public const string BHTownship = "BHTownship";
            public const string ActualBottomTownship = "ActualBottomTownship";
            public const string TargetBottomTownship = "TargetBottomTownship";
            public const string LateralLength = "LateralLength";
            public const string ActualLateralLength = "ActualLateralLength";
            public const string TargetLateralLength = "TargetLateralLength";
            public const string TD = "TD";
            public const string ActualTmd = "ActualTmd";
            public const string TargetTotalMeasuredDepth = "TargetTotalMeasuredDepth";
            public const string TVD = "TVD";
            public const string ActualTvd = "ActualTvd";
            public const string TargetTotalVerticalDepth = "TargetTotalVerticalDepth";

            // Config fields
            //
            public const string ConfigKey = "Title";
            public const string ConfigValue = "ConfigValue";
            public const string ConfigKeyWellStatus = "WellStatus";
        }

        public static class ListUrls
        {
            public const string ConfigSettings = "Lists/ConfigurationSettings";
            public const string MasterWellList = "Lists/AECMasterWellList";
        }

        public static class ListNames
        {
            // Config list
            //
            public const string RequestRedirectionList = "Master Well List";
        }

        public static class SiteGroupNames
        {
            public const string DesignersSiteGroupName = "Designers";
        }

        public static class AudienceNames
        {
            public const string Audience_Employees = "Employees";
        }

        public static class ContentTypeNames
        {
            public const string MandatoryPage = "Mandatory Page";
        }

        public static class PageLayouts
        {
            public const string MandatoryPageLayoutTitle = "Default Mandatory Page";
        }

        public static string[] RelativeUrlPatterns = new string[] {
            @"href=""/",
            @"img=""/",
            @"src=""/",
            @"value=""/"
        };
        #endregion

        #region Color transform methods

        /// <summary>
        /// Converts a hex color to a System.Drawing.Color.
        /// </summary>
        /// <param name="hexColor">e.g. "#EA7125" [RGB], "EA7125" [#RGB], "FFEA7125" [ARGB], "C00" [RGB], "#C00" [RGB]</param>
        public static Color ColorFromHex(string hexColor)
        {
            // Parse RGB hex
            string rgbHexColor = hexColor.Length == 6 ? hexColor : // e.g. EA7125 [RGB]
                hexColor.Length == 7 && hexColor.StartsWith("#") ? hexColor.TrimStart('#') : // eg. #EA7125 [#RGB]
                hexColor.Length == 8 ? hexColor.Substring(2) : // e.g. FFEA7125 [ARGB]
                hexColor.Length == 3 ? (hexColor[0].ToString() + hexColor[0] + hexColor[1] + hexColor[1] + hexColor[2] + hexColor[2]) : // e.g. C00 [RGB]
                hexColor.Length == 4 && hexColor.StartsWith("#") ? (hexColor[1].ToString() + hexColor[1] + hexColor[2] + hexColor[2] + hexColor[3] + hexColor[3]) : // e.g. #C00 [RGB]
                null; // Invalid format
            if (rgbHexColor == null)
            {
                throw new InvalidCastException(string.Format("The supplied hex color value () is of an invalid format.", hexColor));
            }
            // Parse integer RGBA
            int alpha = hexColor.Length == 8 /* ARGB hex */? int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier) : 255;
            int red = int.Parse(rgbHexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(rgbHexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(rgbHexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            Color argbColor = Color.FromArgb(alpha, red, green, blue);

            return argbColor;
        }

        public static Color ColorFromHexAndOpacity(string hexColor, double opacity)
        {
            int alpha = (int)Math.Round(opacity * 255);
            Color rgbColor = ColorFromHex(hexColor);
            Color argbColor = Color.FromArgb(alpha, rgbColor);
            return argbColor;
        }

        /// <summary>
        /// Converts a System.Drawing.Color instance to respective Web hex color and opacity values.
        /// </summary>
        /// <param name="argbColor">System.Drawing.Color instance</param>
        /// <param name="hexColor">e.g. "#EA7125"</param>
        /// <param name="opacity">value between 0 and 1.00</param>
        public static void ColorToHex(Color argbColor, out string hexColor, out double opacity)
        {
            hexColor = "#" + argbColor.Name.Substring(2);
            opacity = Math.Round(((double)argbColor.A / (double)255), 2);
        }

        /// <summary>
        /// Performs a blend of the supplied RGBA color as RGB with alpha over white (similated transparency over white background).
        /// </summary>
        /// <param name="rgbaColor">System.Drawing.Color instance</param>
        public static Color BlendAlphaOverWhiteBackground(Color rgbaColor)
        {
            // opacity*original + (1-opacity)*background = resulting pixel
            int background = 255; // White
            double opacity = (double)rgbaColor.A / 255D;
            int blendedRed = (int)Math.Round(opacity * (double)rgbaColor.R + (1D - opacity) * (double)background);
            int blendedGreen = (int)Math.Round(opacity * (double)rgbaColor.G + (1D - opacity) * (double)background);
            int blendedBlue = (int)Math.Round(opacity * (double)rgbaColor.B + (1D - opacity) * (double)background);
            Color blendColor = Color.FromArgb(blendedRed, blendedGreen, blendedBlue);
            return blendColor;
        }

        #endregion
    }
}
