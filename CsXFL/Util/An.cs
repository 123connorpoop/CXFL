// utility class, simliar to the fl object in JSFL
using System.IO.Compression;
using System.Xml.Linq;

namespace CsXFL;
public static class An
{
    private static readonly Dictionary<string, string> extractedFlas = new();
    private static readonly List<Document> _documents = new();
    private static Document? activeDocument;
    static An()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Cleanup();
    }
    public static async Task<Document> OpenDocumentAsync(string filepath)
    {
        Document doc = await Task.Run(() => new Document(filepath));
        _documents.Add(doc);
        activeDocument = doc;
        return doc;
    }
    public static Document GetDocument(int index)
    {
        return _documents[index];
    }
    public static Document GetActiveDocument()
    {
        return activeDocument ?? throw new InvalidOperationException("No active document");
    }
    public static void CloseDocument(int index)
    {
        _documents.RemoveAt(index);
    }
    public static void ImportFromOtherDocument(this Document doc, string otherDocPath, string itemName)
    {
        if (!File.Exists(otherDocPath)) throw new FileNotFoundException("The file does not exist", otherDocPath);
        HashSet<string> filesToImport = new();
        bool isSymbol = Path.GetExtension(itemName) == ".xml", isFla = Path.GetExtension(otherDocPath) == ".fla";
        if (Path.GetExtension(itemName) == string.Empty)
        {
            itemName += ".xml";
            isSymbol = true;
        }
        if (isFla)
        {
            otherDocPath = GetTempFLAPath(otherDocPath);
        }
        string otherItemPath = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, itemName);
        if (!File.Exists(otherItemPath)) throw new FileNotFoundException("The item does not exist in the other document", otherItemPath);
        if (isSymbol)
        {
            AddSymbolAndDependencies(otherItemPath, filesToImport, otherDocPath);
        }
        filesToImport.Add(otherItemPath.Replace('\\', '/'));
        string otherDocumentLibraryRoot = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH).Replace('\\', '/');
        List<Item?> importedItems = new();
        foreach (string file in filesToImport)
        {
            importedItems.Add(doc.Library.ImportItem(file, true, otherDocumentLibraryRoot));
        }
        // init symbols
        foreach (Item? item in importedItems)
        {
            if (item is SymbolItem symbol)
            {
                _ = symbol.Timeline;
            }
        }
    }

    private static string GetTempFLAPath(string otherDocPath)
    {
        if (!extractedFlas.TryGetValue(otherDocPath, out string? tempDir))
        {
            // extract fla to xfl in temp directory
            tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            while (Directory.Exists(tempDir))
            {
                tempDir = Path.Combine(tempDir, Path.GetRandomFileName());
            }
            Directory.CreateDirectory(tempDir);
            using (ZipArchive archive = ZipFile.Open(otherDocPath, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDir);
            }
            extractedFlas.TryAdd(otherDocPath, tempDir);
        }
        otherDocPath = tempDir + "/DOMDocument.xml";
        return otherDocPath;
    }

    private static void AddSymbolAndDependencies(string symbolPath, HashSet<string> filesToImport, string otherDocPath)
    {
        string toImport = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, symbolPath).Replace('\\', '/');
        if (Path.GetExtension(toImport) == string.Empty) toImport += ".xml";
        if (!filesToImport.Add(toImport)) return;
        if (Path.GetExtension(toImport) != ".xml") return; // not a symbol

        HashSet<string> dependencies = ParseSymbolFile(toImport, filesToImport);

        foreach (string dependency in dependencies)
        {
            AddSymbolAndDependencies(dependency, filesToImport, otherDocPath);
        }
    }
    private static HashSet<string> ParseSymbolFile(string symbolPath, HashSet<string> existingDependencies)
    {
        HashSet<string> dependencies = new();
        SymbolItem symbol = SymbolItem.FromFile(symbolPath);
        foreach (Layer l in symbol.Timeline.Layers)
        {
            foreach (Frame f in l.KeyFrames)
            {
                if(f.SoundName != Frame.DefaultValues.SoundName)
                {
                    dependencies.Add(f.SoundName);
                }
                foreach (Element e in f.Elements)
                {
                    if (e is Instance i)
                    {
                        if (existingDependencies.Contains(i.LibraryItemName)) continue;
                        dependencies.Add(i.LibraryItemName);
                    }
                }
            }
        }
        return dependencies;
    }
    public static void ImportFolderFromOtherDocument(this Document doc, string otherDocPath, string folderName)
    {
        if (!File.Exists(otherDocPath)) throw new FileNotFoundException("The file does not exist", otherDocPath);
        bool isFla = Path.GetExtension(otherDocPath) == ".fla";
        if (isFla)
        {
            otherDocPath = GetTempFLAPath(otherDocPath);
        }
        Document otherDoc = new(otherDocPath);
        string otherFolderPath = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, folderName);
        if (!Directory.Exists(otherFolderPath)) throw new DirectoryNotFoundException($"The folder does not exist in the other document: {folderName}");
        HashSet<string> filesToImport = new();
        foreach (string file in otherDoc.Library.Items.Keys.Where(f => f.StartsWith(folderName + "/")))
        {
            string absoluteFilePath = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH, file).Replace('\\', '/');
            // see if that file exists and append the correct extension
            string[] matchingFiles = Directory.GetFiles(Path.GetDirectoryName(absoluteFilePath)!, Path.GetFileNameWithoutExtension(absoluteFilePath) + ".*");
            if (matchingFiles.Length == 0) continue;
            foreach (string matchingFile in matchingFiles)
            {
                if(filesToImport.Contains(matchingFile)) continue;
                absoluteFilePath = matchingFile.Replace('\\', '/');
            }
            bool isSymbol = Path.GetExtension(absoluteFilePath) == ".xml";
            if (isSymbol)
            {
                AddSymbolAndDependencies(absoluteFilePath, filesToImport, otherDocPath);
            }
            filesToImport.Add(absoluteFilePath);
        }
        string otherDocumentLibraryRoot = Path.Combine(Path.GetDirectoryName(otherDocPath)!, Library.LIBRARY_PATH).Replace('\\', '/');
        List<Item?> importedItems = new();
        foreach (string file in filesToImport)
        {
            importedItems.Add(doc.Library.ImportItem(file, true, otherDocumentLibraryRoot));
        }
        // init symbols
        foreach (Item? item in importedItems)
        {
            if (item is SymbolItem symbol)
            {
                _ = symbol.Timeline;
            }
        }
    }
    public static bool ImportSceneFromOtherDocument(this Document doc, Document otherDoc, int sceneIndex)
    {
        string otherDocPath = otherDoc.Filename;
        bool isFla = Path.GetExtension(otherDocPath) == ".fla";
        string updatedPath = otherDocPath;
        if (isFla)
        {
            updatedPath = GetTempFLAPath(otherDocPath);
        }
        XNamespace ns = doc.Root!.Name.Namespace;
        Timeline duped = new(otherDoc.GetTimeline(sceneIndex));
        duped.Name += " (imported)";
        if (duped.Root?.Parent is not null)
            duped.Root?.Remove();
        if (doc.Root?.Element(ns + Timeline.TIMELINES_NODEGROUP_IDENTIFIER) is null) doc.Root?.Add(new XElement(ns + Timeline.TIMELINES_NODEGROUP_IDENTIFIER));
        doc.Root?.Element(ns + Timeline.TIMELINES_NODEGROUP_IDENTIFIER)!.Add(duped.Root);
        doc.Timelines.Add(duped);
        // now import all the library items
        HashSet<string> filesToImport = new();
        foreach (Layer l in duped.Layers)
        {
            foreach (Frame f in l.KeyFrames)
            {
                if(f.SoundName != Frame.DefaultValues.SoundName)
                {
                    filesToImport.Add(f.SoundName);
                }
                foreach (Element e in f.Elements)
                {
                    if (e is Instance i)
                    {
                        filesToImport.Add(i.LibraryItemName);
                    }
                }
                if (!string.IsNullOrEmpty(f.SoundName))
                {
                    filesToImport.Add(f.SoundName);
                }
            }
        }
        foreach (string file in filesToImport)
        {
            if (!doc.Library.ItemExists(file))
            {
                doc.ImportFromOtherDocument(updatedPath, file);
            }
        }
        return true;
    }
    public static bool ImportSceneFromOtherDocument(this Document doc, string otherDocPath, int sceneIndex)
    {
        Document otherDoc = new(otherDocPath);
        return doc.ImportSceneFromOtherDocument(otherDoc, sceneIndex);
    }
    private static void Cleanup()
    {
        foreach (string tempDir in extractedFlas.Values)
        {
            Directory.Delete(tempDir, true);
        }
        extractedFlas.Clear();
    }
    public static Document CreateDocument(string path)
    {
        // create the xfl format: DOMDocument.xml, LIBRARY folder, etc.
        throw new NotImplementedException();
    }
}