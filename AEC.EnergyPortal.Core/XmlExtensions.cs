using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Xsl;
using System.IO;

namespace AEC.EnergyPortal.Core
{
    public static class XmlExtensions
    {

        public static XElement GetXElement(this XmlNode node)
        {
            var xDoc = new XDocument();
            using (var xmlWriter = xDoc.CreateWriter())
                node.WriteTo(xmlWriter);
            return xDoc.Root;
        }


        public static XmlNode GetXmlNode(this XNode node)
        {
            using (var xmlReader = node.CreateReader())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);
                return xmlDoc;
            }
        }

        public static XmlDocument GetXmlDocument(this XDocument node)
        {
            return GetXmlNode(node) as XmlDocument;
        }

        /// <summary>
        /// Transform xml document by applying xsl stylesheet
        /// </summary>
        /// <param name="inDoc">The document to be transformed</param>
        /// <param name="stylesheet">The stylesheet to be applied</param>
        /// <returns>Transformed xml document</returns>
        public static string Transform(this XmlDocument doc, XmlDocument stylesheet)
        {
            var xslt = new XslCompiledTransform();
            var settings = new XsltSettings(true, true);
            xslt.Load(stylesheet, settings, new XmlUrlResolver());
            var reader = new XmlNodeReader(doc);
            var outDoc = new StringBuilder();

            using (var writer = new StringWriter(outDoc))
                xslt.Transform(reader, null, writer);

            return outDoc.ToString();
        }

        /// <summary>
        /// Transform xml document by applying xsl stylesheet
        /// </summary>
        /// <param name="doc">The document to be transformed</param>
        /// <param name="stylesheet">The stylesheet to be applied</param>
        /// <returns>Transformed xml document</returns>
        public static string Transform(this XmlDocument doc, string stylesheet)
        {
            var xslDoc = new XmlDocument();
            xslDoc.LoadXml(stylesheet);
            return Transform(doc, xslDoc);
        }

        /// <summary>
        /// Transform xml document by applying xsl stylesheet
        /// </summary>
        /// <param name="doc">The document to be transformed</param>
        /// <param name="stylesheet">The stylesheet to be applied</param>
        /// <returns>Transformed xml document</returns>
        public static string XslTransform(this string doc, string stylesheet)
        {
            var xslDoc = new XmlDocument();
            xslDoc.LoadXml(stylesheet);
            var srcDoc = new XmlDocument();
            srcDoc.LoadXml(doc);
            return Transform(srcDoc, xslDoc);
        }

    }
}