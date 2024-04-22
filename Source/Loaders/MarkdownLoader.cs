using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using Verse;

namespace MoreDefLanguages.Loaders;

public class MarkdownLoader : ILoader
{
    public XmlDocument Parse(string filePath)
    {
        var contents = File.ReadAllText(filePath);
        var result = CommonMark.CommonMarkConverter.Convert(contents);

        var sourceDocument = new HtmlDocument();
        sourceDocument.LoadHtml(result);

        var xmlDoc = Parse(sourceDocument);
        var xml = xmlDoc.OuterXml;
        return xmlDoc;
    }

    public XmlDocument Parse(HtmlDocument document)
    {
        var xmlDoc = new XmlDocument();
        var defs = xmlDoc.AppendChild(xmlDoc.CreateElement("Defs"));

        var topNodes = document.DocumentNode.ChildNodes
            .Where(node => !(node is HtmlTextNode && node.InnerHtml == "\r\n")).ToList();

        XmlElement workingElement = null;

        topNodes.ForEach(node =>
        {
            switch (node.Name)
            {
                case "h2":
                    var name = node.InnerHtml;
                    var match = Regex.Match(name, @"^(.*?)\s*?\[(.*?)\]$");
                    var attributes = new List<string>();
                    if (match.Success)
                    {
                        name = match.Groups[1].Value;
                        attributes = match.Groups[2].Value.Split(',').ToList();
                    }
                    workingElement = defs.AppendChild(xmlDoc.CreateElement(name)) as XmlElement;

                    if (attributes.Count > 0)
                    {
                        attributes.ForEach(rawAttr =>
                        {
                            var splitAttr = rawAttr.Split('=');
                            if (splitAttr.Length != 2) return;
                        
                            workingElement.Attributes.Append(xmlDoc.CreateAttribute(splitAttr[0])).InnerText = splitAttr[1];
                        });
                    }
                    
                    break;
                case "ul":
                    BuildDef(node, workingElement, xmlDoc);
                    break;
            }
        });

        return xmlDoc;
    }

    public XmlElement BuildDef(HtmlNode currentNode, XmlElement parent, XmlDocument doc)
    {
        var children = currentNode.ChildNodes.Where(node => !(node is HtmlTextNode && node.InnerHtml == "\r\n"))
            .ToList();

        children.ForEach(child =>
        {
            var innerChildren = child.ChildNodes.Where(node => !(node is HtmlTextNode && node.InnerHtml == "\r\n"))
                .ToList();
            XmlElement workingElement = null;
            foreach (var realChild in innerChildren)
            {
                if (realChild is not HtmlTextNode)
                {
                    if (realChild.Name != "ul") continue;
                    if (workingElement is null) continue;

                    BuildDef(realChild, workingElement, doc);
                    workingElement = null;
                    continue;
                }

                var innerHtml = realChild.InnerHtml.Replace("\r\n", "");
                var match = Regex.Match(innerHtml, "^\\[(.*?)\\]( .*?)?$");
                if (!match.Success) continue;

                var nodeName = match.Groups[1].Value;
                var attributes = new List<string>();
                
                if (nodeName.Contains(","))
                {
                    attributes = nodeName.Split(',').Select(attr => attr.Trim()).ToList();
                    nodeName = attributes.First();
                    attributes.Remove(nodeName);
                }

                var newElement = parent.AppendChild(doc.CreateElement(nodeName));

                if (attributes.Count > 0)
                {
                    attributes.ForEach(rawAttr =>
                    {
                        var splitAttr = rawAttr.Split('=');
                        if (splitAttr.Length != 2) return;
                        
                        newElement.Attributes.Append(doc.CreateAttribute(splitAttr[0])).InnerText = splitAttr[1];
                    });
                }
                
                if (!match.Groups[2].Value.NullOrEmpty())
                {
                    var nodeValue = match.Groups[2].Value;
                    newElement.InnerText = nodeValue.Trim();
                }
                else
                {
                    workingElement = newElement as XmlElement;
                }
            }
        });
        return null;
    }
}