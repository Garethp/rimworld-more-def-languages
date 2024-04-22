using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace MoreDefLanguages.Loaders;

public class JSONLoader: ILoader
{
    public XmlDocument Parse(string filePath)
    {
        var document = new XmlDocument();
        
        var contents = File.ReadAllText(filePath);

        var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(contents), new XmlDictionaryReaderQuotas());
        var root = XElement.Load(jsonReader);
        var nodes = root.Nodes().ToList();
        
        var defs = document.AppendChild(document.CreateElement("Defs"));
        
        foreach (var xNode in nodes)
        {
            ParseNode(xNode, defs);
        }
        
        var xml = document.OuterXml;
        return document;
    }
    
    private XmlNode ParseNode(XNode source, XmlNode target, bool isList = false)
    {
        if (source is not XElement sourceElement) return null;
        if (sourceElement.Attribute("type") is not { } typeAttr) return null;
        
        if (sourceElement.Name.LocalName.StartsWith("Attr::"))
        {
            if (typeAttr.Value != "string") return target;
            
            var name = sourceElement.Name.LocalName.Replace("Attr::", "");
            // target.Attributes.Append(target.OwnerDocument.CreateAttribute(name, sourceElement.Value));
            
            return target;
        };
        
        var element = target.AppendChild(target.OwnerDocument.CreateElement(isList ? "li" : sourceElement.Name.LocalName));
        // var element = new XmlDocument().CreateElement(sourceElement.Name.LocalName);
        
        switch (typeAttr.Value)
        {
            case "object":
                foreach (var xNode in sourceElement.Nodes())
                {
                    ParseNode(xNode, element);
                }
                break;
            case "string":
                if (sourceElement.Attribute("item") is { } itemAttr && itemAttr.Value.StartsWith("Attr:"))
                {
                    var name = itemAttr.Value.Replace("Attr::", "");
                    var attribute = target.Attributes.Append(target.OwnerDocument.CreateAttribute(name));
                    attribute.Value = sourceElement.Value;
                    target.RemoveChild(element);
                    break;
                }
                
                element.InnerText = sourceElement.Value;
                break;
            case "array":
                foreach (var xNode in sourceElement.Nodes())
                {
                    ParseNode(xNode, element, true);
                }                
                break;
        }
        
        return element;
    }
}