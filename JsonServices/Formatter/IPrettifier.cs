namespace JsonServices.Formatter;

public interface IPrettifier
{
    string PrettifyJson();
    string PrettifyXml();
    Prettifier SetText(string text);
    bool IsValidXml(string xmlString);
    bool IsValidJson(string jsonString);
}