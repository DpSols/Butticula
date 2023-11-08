namespace JsonServices;

public class FileTempService : IFileService
{
    public string GetFilePath(string file) => Path.Combine(@"C:\MyTemp", file + ".json");

    public void FileWriter(string textContent, string fileName)
    {
        var stream = File.Create(GetFilePath(fileName));
        using (StreamWriter outputFile = new StreamWriter(stream))
        {
            outputFile.WriteLineAsync(textContent);
            outputFile.Close();
        }
    }

    public string FileReader(string fileName)
    {
        using (StreamReader inputFile = new StreamReader(GetFilePath(fileName)))
        {
            String line = inputFile.ReadToEnd();
            return line;
        }
    }

    public void FileRemove(string fileName)
    {
        var file = new FileInfo(GetFilePath(fileName));
        file.Delete();
    }
}