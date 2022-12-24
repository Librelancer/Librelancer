using System.IO;
using System.Xml.Serialization;

namespace InterfaceEdit;

[XmlRoot("InterfaceProject")]
public class ProjectConfiguration
{
    static XmlSerializer _xml = new XmlSerializer(typeof(ProjectConfiguration));

    public string DataFolder { get; set; }
    public string OutputFilename { get; set; }

    public static ProjectConfiguration Read(string filename)
    {
        using var reader = new StreamReader(filename);
        return (ProjectConfiguration)_xml.Deserialize(reader);
    }

    public void Write(string filename)
    {
        using var writer = new StreamWriter(filename);
        _xml.Serialize(writer, this);
    }
}