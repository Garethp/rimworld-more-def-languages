using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Verse;

namespace MoreDefLanguages.Loaders;

public class BetterCSVLoader : ILoader
{
    public XmlDocument Parse(string filePath)
    {
        var contents = File.ReadAllLines(filePath);

        var definition = ParseCSV(contents);

        var document = new XmlDocument();
        var root = document.AppendChild(document.CreateElement("Defs"));

        definition.Values.ToList().ForEach(row =>
        {
            if (!row.ContainsKey("DefType")) return;

            var def = root.AppendChild(document.CreateElement(row["DefType"]));

            foreach (var (key, value) in row)
            {
                if (key == "DefType") continue;

                var name = key;

                var path = name.Split('/').ToList();
                name = path.Pop();
                
                if (name.StartsWith("Attr::") && path.Count == 0)
                {
                    var attribute = def.Attributes.Append(document.CreateAttribute(name.Replace("Attr::", "")));
                    attribute.InnerText = value;
                    continue;
                }

                XmlElement element = null;

                if (path.Count > 0)
                {
                    var parent = def;
                    foreach (var pathElement in path)
                    {
                        var pathElementName = pathElement;
                        var match = Regex.Match(pathElementName, @"li\[(\d+)\]");
                        XmlElement possibleNewParent = null;

                        if (match.Success)
                        {
                            var index = int.Parse(match.Groups[1].Value);
                            pathElementName = "li";
                            var possibleParents = parent.SelectNodes(pathElementName);
                            if (possibleParents.Count > index) possibleNewParent = possibleParents[index] as XmlElement;
                        }
                        else
                        {
                            possibleNewParent = parent.SelectSingleNode(pathElementName) as XmlElement;
                        }

                        if (possibleNewParent == null)
                        {
                            parent = parent.AppendChild(document.CreateElement(pathElementName)) as XmlElement;
                        }
                        else
                        {
                            parent = possibleNewParent;
                        }
                    }

                    if (name.StartsWith("Attr::"))
                    {
                        var attribute = parent.Attributes.Append(document.CreateAttribute(name.Replace("Attr::", "")));
                        attribute.InnerText = value;
                        continue;
                    }
                    
                    if (name.StartsWith("li[")) name = "li";
                    element = parent.AppendChild(document.CreateElement(name)) as XmlElement;
                }
                else
                {
                    if (name.StartsWith("li[")) name = "li";
                    element = def.AppendChild(document.CreateElement(name)) as XmlElement;
                }

                element.InnerText = value;
            }
        });

        var xml = document.OuterXml;
        return document;
    }
    
    public static Dictionary<int, Dictionary<string, string>> ParseCSV(string[] contents)
    {
        var csv = new Dictionary<int, Dictionary<string, string>>();
        var headers = new List<string>();
        var resetHeaders = true;

        for (var i = 0; i < contents.Length; i++)
        {
            if (contents[i] == "---")
            {
                resetHeaders = true;
                continue;
            }

            if (resetHeaders)
            {
                headers = contents[i].Split(',').ToList();
                resetHeaders = false;
                continue;
            }

            var row = new Dictionary<string, string>();
            var values = new List<string>();
            var value = "";

            var inQuotes = false;
            for (var characterIndex = 0; characterIndex < contents[i].Length; characterIndex++)
            {
                if (contents[i][characterIndex] == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (contents[i][characterIndex] == ',' && !inQuotes)
                {
                    values.Add(value);
                    value = "";
                    continue;
                }

                value += contents[i][characterIndex];
            }

            values.Add(value);

            for (int j = 0; j < headers.Count; j++)
            {
                if (values.Count > j)
                {
                    row.Add(headers[j], values[j]);
                }
            }

            csv.Add(i + 1, row);
        }

        return csv;
    }
}