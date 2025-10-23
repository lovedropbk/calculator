using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FinancialCalculator.Engine.Core;

public interface IFileService
{
    bool Exists(string path);
    Task<string> ReadAllTextAsync(string path);
    Task<string[]> ReadAllLinesAsync(string path);
    TextReader OpenText(string path);
}

public class FileService : IFileService
{
    public bool Exists(string path) => File.Exists(path);
    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);
    public Task<string[]> ReadAllLinesAsync(string path) => File.ReadAllLinesAsync(path);
    public TextReader OpenText(string path) => File.OpenText(path);
}