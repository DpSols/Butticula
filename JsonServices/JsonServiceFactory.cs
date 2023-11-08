using JsonServices.Formatter;

namespace JsonServices;

public class JsonServiceFactory
{
    public IPrettifier GetPrettifier => new Prettifier();
    public IFileService GetFileService => new FileTempService();
}