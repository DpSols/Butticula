using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace JsonServices.Formatter;

public class Prettifier : IPrettifier
{
    private string _inputText;

    public Prettifier SetText(string text)
    {
        _inputText = text;

        return this;
    }
    
    public string PrettifyJson()
    {
        try
        {
            return JToken.Parse(_inputText).ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public string PrettifyXml()
    {
        try
        {
            return XDocument.Parse(_inputText).ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public bool IsValidXml(string xmlString)
    {
        try
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }
    
    public bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}