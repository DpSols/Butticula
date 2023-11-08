using Newtonsoft.Json.Linq;

namespace JsonServices.Formatter;

public class Prettifier : IPrettifier
{
    private string _inputJson;

    public Prettifier SetText(string json)
    {
        _inputJson = json;

        return this;
    }
    
    public string Prettify()
    {
        try
        {
            return JToken.Parse(_inputJson).ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}