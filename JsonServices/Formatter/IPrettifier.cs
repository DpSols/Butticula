namespace JsonServices.Formatter;

public interface IPrettifier
{
    string Prettify();
    Prettifier SetText(string json);
}