# More Def Languages

Don't use this. I need several showers after writing this. I'm sorry.

## What does this do?

This allows you to define your Rimworld Defs in more than just XML. Specifically, it implements support for writing Defs
in CSV, JSON and Markdown format. It also exposes a method to add your own `Loader`, allowing you to implement more
langauges for Defs to be defined in if you really want to.

## How does it work?

In [`Source/Patches/XmlAssetsInModFolderLoader.cs`](`Source/Patches/XmlAssetsInModFolderLoader.cs`) we use Harmony to
patch into `Verse.DirectXmlLoader::XmlAssetsInModFolder`, creating a Postfix where we search the Def folder for files
matching our new custom extensions. We then run them through a custom "Loader" which accepts a `string filePath` and
returns an `XmlDocument`. This `XmlDocument` should be in the same format as the XML files that Rimworld expects. This
means that as far as Rimworld is concerned the Defs are still in XML format and can be interacted with/patched as normal.

## How do I write a custom Loader

Write a class that implements:

```cs
public interface ILoader
{
    [CanBeNull]
    public XmlDocument Parse(string filePath);
}
```

And then call `DefLanguages.AddLoader(".ext", new YourLoader());` in your mod's `OnStartup` method. Don't worry, it's a
static method so you can call it from anywhere.

## How do I write a Def in a new language?

It's slightly different syntax for different languages

### JSON

Despite the fact that strict JSON should only allow for unqiue keys, we allow for multiple `ThingDef` objects in a single
root. If you want to define attributes for an element (Such as `Parent` on a def or `Class` on a comp) you define a key
in that object with the format `Attr::Key`. Otherwise it's perfectly ordinary looking JSON

```json
{
  "ThingDef": {
    "defName": "myDef",
    "label": "MyDef",
    "comps": [
      {
        "Attr::Class": "CompProperties",
        "SomeKey": "SomeValue"
      }
    ]
  },
  "ThingDef": {
    "Attr::Parent": "BaseClass",
    "defName": "myDef2",
    "label": "MyDef2",
    "tags": [
      "tag1",
      "tag2"
    ]
  }
}
```

### Yaml

YAML follow the same structure as JSON, except for the fact that it does not allow multiple `ThingDef` objects in the
same root. For that reason, your `ThingDefs` can either be a single object or an array of objects. Your file can end
with either `.yaml` or `.yml`

```yaml
ThingDef:
  - defName: myDef
    "Attr:SomeAttr": test
    label: MyDef
    comps:
      - "Attr::Class": "CompProperties"
        SomeKey: SomeValue
      
  - defName: myDef2
    label: MyDef2
    tags:
      - tag1
      - tag2
```

### Markdown

This is fairly straight forward. We define a top level header (One `#`) with the key `Defs`. Then, we'll have one
header for each Def with it's type as the Header. For the attributes, we define them as lists and nested lists, in a
`[key] Value` format. If we want to define attributes, there are two separate formats. The first is attributes for the
Def itself, which is to put `[AttributeName=AttributeValue]` **after** the Def type. The second is for attributes inside
the def on the properties. We do this with the format `[key,Attribute1=AttributeValue1,Attribute2=AttributeValue2] Value`.

The rest should be self explanatory below

```md
# Defs

## ThingDef [Parent=SomeDef]

* [defName] MyCustomDef
* [label] A custom def that I built
* [graphicData]
    * [texPath] Things/Shuttle/shuttle_personal
    * [graphicClass] Graphic_Single
    * [drawSize] (2,2)
    * [shadowData]
        * [volume] (0.75, 0.35, 0.33)
* [tags]
    * [li] Tag 1
    * [li] Tag 2
* [comps]
  * [li,Class=Something]
    * [compClass] CompProperties
```

### CSV

CSV expects standard CSV format, with the first row being the headers. You must define a header by the name `DefType`
and use the value to define what the type is. Attributes are mostly the same as in JSON: Define a key with the format
`Attr::Key` to define an attribute. Nested properties (such as `comps`) are handled by defining the key with slashes as
the delimiter for the path that you want to define. For example, if you want to define `graphicsData.drawSize`, your header
would be `graphicData/drawSize`. If you want to define `<graphicsData Attr="Value">`, then your header would be
`graphcisData/Attr:Attr`. Finally, for `li`, append `[{index}]` to the end of `li` to keep your list elements unique.

Since this is CSV, headers are defined in the first row. If you want to define different types of defs, you'll need to split
then into different files.

```csv
DefType,Attr::Parent,defName,label,description,drawerType,graphicData/texPath,graphicData/graphicClass,graphicData/drawSize,graphicData/shadowData/volume,tags/li[0],tags/li[1],comps/li[0]/Attr::Class,comps/li[0]/CompProperty
ThingDef,Base,ShuttleCasket,Shuttle Casket,A place to put the dead and send them into the vastness of the ocean.,MapMeshAndRealTime,Things/Shuttle/shuttle_personal,SINGLE,"(2,2)","(0.75, 0.35, 0.33)",tag 1,tag 2,CompClass,SomeValue
```