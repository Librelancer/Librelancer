using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LancerEdit.GameContent;

public static class XmlFormatter
{
    public static string Prettify(string xml)
    {
        try
        {
            return PrettyXml(xml);
        }
        catch (Exception)
        {
            return xml;
        }
    }

    public static string Minimize(string xml)
    {
        try
        {
            return MinimizeXml(xml);
        }
        catch (Exception)
        {
            return xml;
        }
    }

    static string MinimizeXml(string xml)
    {
        var stringBuilder = new StringBuilder();

        var element = XElement.Parse(xml);

        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = false;
        settings.Indent = false;
        settings.Encoding = Encoding.Unicode;
        
        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    }
    static string PrettyXml(string xml)
    {
        var stringBuilder = new StringBuilder();

        var element = XElement.Parse(xml);

        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;
        settings.NewLineOnAttributes = false;
        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    } 
}