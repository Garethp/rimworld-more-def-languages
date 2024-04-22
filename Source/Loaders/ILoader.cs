using System.Xml;
using JetBrains.Annotations;

namespace MoreDefLanguages.Loaders;

public interface ILoader
{
    [CanBeNull]
    public XmlDocument Parse(string filePath);
}