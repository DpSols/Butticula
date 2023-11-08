namespace JsonServices;

public interface IFileService
{
    string GetFilePath(string file);
    void FileWriter(string textContent, string fileName);
    string FileReader(string fileName);
    void FileRemove(string fileName);
}