using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using YamlDotNet.Serialization;

namespace MoreDefLanguages.Loaders;

public class YamlLoader: ILoader
{
    public XmlDocument Parse(string filePath)
    {
        var document = new XmlDocument();
        
        var contents = File.ReadAllText(filePath);

        var yamlDeserialiser = new DeserializerBuilder().Build();
        var yamlDoc = yamlDeserialiser.Deserialize(contents);

        var defsContainer = document.AppendChild(document.CreateElement("Defs"));

        if (yamlDoc is Dictionary<object, object> defsDict)
        {
            var defTypes = defsDict.Keys.Where(k => k is string).ToList();

            foreach (var defType in defTypes)
            {
                var defs = new List<Dictionary<object, object>>();
                switch (defsDict[defType])
                {
                    case Dictionary<object, object> singleDef:
                        defs = new List<Dictionary<object, object>>([singleDef]);
                        break;
                    case List<object> defList:
                        defList.ForEach(item =>
                        {
                            if (item is Dictionary<object, object> def)
                                defs.Add(def);
                        });
                        break;
                }

                foreach (var def in defs)
                {
                    var element = defsContainer.AppendChild(defsContainer.OwnerDocument.CreateElement(defType.ToString()));
                    ParseNode(def, element);
                }
            }
        }
        
        var xml = document.OuterXml;
        return document;
    }

    private void ParseNode(Dictionary<object, object> source, XmlNode target, bool isList = false)
    {
        foreach (var initialKey in source.Keys)
        {
            if (initialKey is not string key)
                continue;
            
            var element = target.AppendChild(target.OwnerDocument.CreateElement(isList || key.Contains("::") ? "li" : key));
            var value = source[key];

            switch (value)
            {
                case string stringValue when key.StartsWith("Attr:"):
                {
                    var name = key.Replace("Attr::", "");
                    var attribute = target.Attributes.Append(target.OwnerDocument.CreateAttribute(name));
                    attribute.Value = stringValue;
                    target.RemoveChild(element);
                    continue;
                }
                case string stringValue:
                    element.InnerText = stringValue;
                    continue;
                case List<object> listValue:
                {
                    foreach (var listItem in listValue)
                    {
                        switch (listItem)
                        {
                            case Dictionary<object, object> nestedDict:
                                ParseNode(nestedDict, element, true);
                                continue;
                            case string listItemString:
                            {
                                var listElement = element.AppendChild(element.OwnerDocument.CreateElement("li"));
                                listElement.InnerText = listItemString;
                                continue;
                            }
                            default:
                                throw new Exception("Unknown list item type");
                        }
                    }

                    continue;
                }
                default:
                    throw new Exception("Unknown value type");
            }
        }
    }
}