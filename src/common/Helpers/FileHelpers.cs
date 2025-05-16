using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

public class FileHelpers
{
    public static IEnumerable<string> FindFiles(string path, string pattern)
    {
        var combined = PathHelpers.Combine(path, pattern);
        return combined != null
            ? FindFiles(combined)
            : Enumerable.Empty<string>();
    }

    public static IEnumerable<string> FindFiles(string fileNames)
    {
        var currentDir = Directory.GetCurrentDirectory();
        foreach (var item in fileNames.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            ConsoleHelpers.WriteDebugLine($"  Searching for files '{fileNames}'");

            var i1 = item.LastIndexOf(Path.DirectorySeparatorChar);
            var i2 = item.LastIndexOf(Path.AltDirectorySeparatorChar);
            var hasPath = i1 >= 0 || i2 >= 0;

            var pathLen = Math.Max(i1, i2);
            var path = !hasPath ? currentDir : item.Substring(0, pathLen);
            var pattern = !hasPath ? item : item.Substring(pathLen + 1);

            EnumerationOptions? recursiveOptions = null;
            if (path.EndsWith("**"))
            {
                path = path.Substring(0, path.Length - 2).TrimEnd('/', '\\');
                if (string.IsNullOrEmpty(path)) path = ".";
                recursiveOptions = new EnumerationOptions() { RecurseSubdirectories = true };
            }

            if (!Directory.Exists(path)) continue;

            var files = recursiveOptions != null 
                ? Directory.EnumerateFiles(path, pattern, recursiveOptions)
                : Directory.EnumerateFiles(path, pattern);
            foreach (var file in files)
            {
                yield return file;
            }
        }

        yield break;
    }

    public static IEnumerable<string> FindFilesInOsPath(string fileName)
    {
        var lookIn = Environment.GetEnvironmentVariable("PATH")!.Split(System.IO.Path.PathSeparator);
        var found = lookIn.SelectMany(x =>
        {
            try
            {
                return System.IO.Directory.GetFiles(x, fileName);
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        });
        return found;
    }

    public static string? FindFileSearchParents(params string[] paths)
    {
        return FindFileSearchParents(paths, createIfNotFound: false);
    }

    public static string? FindFileSearchParents(string[] paths, bool createIfNotFound)
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            var combined = Path.Combine(paths.Prepend(current).ToArray());
            if (File.Exists(combined))
            {
                return combined;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        if (createIfNotFound)
        {
            current = Directory.GetCurrentDirectory();
            var combined = Path.Combine(paths.Prepend(current).ToArray());
            DirectoryHelpers.EnsureDirectoryForFileExists(combined);
            WriteAllText(combined, string.Empty);
            return combined;
        }

        return null;
    }

    public static bool FileExists(string? fileName)
    {
        return !string.IsNullOrEmpty(fileName) && (File.Exists(fileName) || fileName == "-");
    }

    public static bool IsFileMatch(string fileName, List<Regex> includeFileContainsPatternList, List<Regex> excludeFileContainsPatternList)
    {
        var checkContent = includeFileContainsPatternList.Any() || excludeFileContainsPatternList.Any();
        if (!checkContent) return true;

        try
        {
            ConsoleHelpers.DisplayStatus($"Processing: {fileName} ...");

            var content = ReadAllText(fileName);
            var includeFile = includeFileContainsPatternList.All(regex => regex.IsMatch(content));
            var excludeFile = excludeFileContainsPatternList.Count > 0 && excludeFileContainsPatternList.Any(regex => regex.IsMatch(content));

            return includeFile && !excludeFile;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ConsoleHelpers.DisplayStatusErase();
        }
    }

    public static string? GetFileNameFromTemplate(string fileName, string? template)
    {
        if (string.IsNullOrEmpty(template)) return template;

        var filePath = Path.GetDirectoryName(fileName);
        var fileBase = Path.GetFileNameWithoutExtension(fileName);
        var fileExt = Path.GetExtension(fileName).TrimStart('.');
        var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

        ConsoleHelpers.WriteDebugLine($"filePath: {filePath}");
        ConsoleHelpers.WriteDebugLine($"fileBase: {fileBase}");
        ConsoleHelpers.WriteDebugLine($"fileExt: {fileExt}");
        ConsoleHelpers.WriteDebugLine($"timeStamp: {timeStamp}");

        return template
            .Replace("{fileName}", fileName)
            .Replace("{filename}", fileName)
            .Replace("{filePath}", filePath)
            .Replace("{filepath}", filePath)
            .Replace("{fileBase}", fileBase)
            .Replace("{filebase}", fileBase)
            .Replace("{fileExt}", fileExt)
            .Replace("{fileext}", fileExt)
            .Replace("{timeStamp}", timeStamp)
            .Replace("{timestamp}", timeStamp)
            .Replace("{time}", time)
            .TrimEnd(' ', '/', '\\');
    }

    public static void ReadIgnoreFile(string ignoreFile, out List<string> excludeGlobs, out List<Regex> excludeFileNamePatternList)
    {
        ConsoleHelpers.WriteDebugLine($"ReadIgnoreFile: ignoreFile: {ignoreFile}");

        excludeGlobs = new List<string>();
        excludeFileNamePatternList = new List<Regex>();

        var fi = new FileInfo(ignoreFile);
        if (!fi.Exists) return;

        var lines = ReadAllLines(ignoreFile);
        foreach (var line in lines)
        {
            var assumeIsGlob = line.Contains('/') || line.Contains('\\');
            if (assumeIsGlob)
            {
                var excludeGlob = PathHelpers.Combine(fi.DirectoryName!, line) ?? line;
                ConsoleHelpers.WriteDebugLine($"ReadIgnoreFile; ignore glob: {excludeGlob}");
                excludeGlobs.Add(excludeGlob!);
            }
            else
            {
                ConsoleHelpers.WriteDebugLine($"ReadIgnoreFile; exclude pattern: {line}");
                excludeFileNamePatternList.Add(new Regex(line));
            }
        }
    }

    public static IEnumerable<string> FilesFromGlobs(List<string> globs)
    {
        foreach (var glob in globs)
        {
            foreach (var file in FilesFromGlob(glob))
            {
                yield return file;
            }
        }
    }

    public static IEnumerable<string> FilesFromGlob(string glob)
    {
        ConsoleHelpers.DisplayStatus($"Finding files: {glob} ...");
        try
        {
            if (glob == "-") return [ glob ]; // special case for stdin

            var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();
            matcher.AddInclude(MakeRelativePath(glob));

            var directoryInfo = new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(Directory.GetCurrentDirectory()));
            var matchResult = matcher.Execute(directoryInfo);

            return matchResult.Files.Select(file => MakeRelativePath(Path.Combine(Directory.GetCurrentDirectory(), file.Path)));
        }
        catch (Exception)
        {
            return Enumerable.Empty<string>();
        }
        finally
        {
            ConsoleHelpers.DisplayStatusErase();
        }
    }

    public static IEnumerable<string> FindMatchingFiles(
        List<string> globs,
        List<string>? excludeGlobs = null,
        List<Regex>? excludeFileNamePatternList = null,
        List<Regex>? includeFileContainsPatternList = null,
        List<Regex>? excludeFileContainsPatternList = null)
    {
        excludeGlobs ??= new List<string>();
        excludeFileNamePatternList ??= new List<Regex>();
        includeFileContainsPatternList ??= new List<Regex>();
        excludeFileContainsPatternList ??= new List<Regex>();
        
        var excludeFiles = new HashSet<string>(FilesFromGlobs(excludeGlobs));
        var files = FilesFromGlobs(globs)
            .Where(file => !excludeFiles.Contains(file))
            .Where(file => !excludeFileNamePatternList.Any(regex => regex.IsMatch(Path.GetFileName(file))))
            .ToList();

        ConsoleHelpers.WriteDebugLine($"DEBUG: 1: Found files ({files.Count()}): ");
        files.ForEach(x => ConsoleHelpers.WriteDebugLine($"DEBUG: 1: - {x}"));
        ConsoleHelpers.WriteDebugLine("");

        if (files.Count == 0)
        {
            ConsoleHelpers.WriteLine($"## Pattern: {string.Join(" ", globs)}\n\n - No files found\n");
            return Enumerable.Empty<string>();
        }

        var filtered = files.Where(file => IsFileMatch(file, includeFileContainsPatternList, excludeFileContainsPatternList)).ToList();
        if (filtered.Count == 0)
        {
            ConsoleHelpers.WriteLine($"## Pattern: {string.Join(" ", globs)}\n\n - No files matched criteria\n");
            return Enumerable.Empty<string>();
        }

        var distinct = filtered.Distinct().ToList();
        ConsoleHelpers.WriteDebugLine($"DEBUG: 2: Found files ({distinct.Count()} distinct/filtered): ");
        distinct.ForEach(x => ConsoleHelpers.WriteDebugLine($"DEBUG: 2: - {x}"));

        return distinct;
    }

    public static string MakeRelativePath(string fullPath)
    {
        var currentDirectory = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(currentDirectory.Length);
        }

        Uri fullPathUri = new Uri(fullPath);
        Uri currentDirectoryUri = new Uri(currentDirectory);

        string relativePath = Uri.UnescapeDataString(currentDirectoryUri.MakeRelativeUri(fullPathUri).ToString().Replace('/', Path.DirectorySeparatorChar));

        if (Path.DirectorySeparatorChar == '\\')
        {
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    public static string ReadAllText(string fileName)
    {
        var content = ConsoleHelpers.IsStandardInputReference(fileName)
            ? string.Join("\n", ConsoleHelpers.GetAllLinesFromStdin())
            : File.ReadAllText(fileName, Encoding.UTF8);

        return content;
    }

    public static string[] ReadAllLines(string fileName)
    {
        var lines = ConsoleHelpers.IsStandardInputReference(fileName)
            ? ConsoleHelpers.GetAllLinesFromStdin().ToArray()
            : File.ReadAllLines(fileName, Encoding.UTF8);

        return lines;
    }

    public static string WriteAllText(string fileName, string content, string? saveToFolderOnAccessDenied = null)
    {
        try
        {
            DirectoryHelpers.EnsureDirectoryForFileExists(fileName);
            File.WriteAllText(fileName, content, Encoding.UTF8);
        }
        catch (Exception)
        {
            var trySavingElsewhere = !string.IsNullOrEmpty(saveToFolderOnAccessDenied);
            if (trySavingElsewhere)
            {
                var userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var trySavingToFolder = Path.Combine(userProfileFolder, saveToFolderOnAccessDenied!);

                var fileNameWithoutFolder = Path.GetFileName(fileName);
                fileName = Path.Combine(trySavingToFolder, fileNameWithoutFolder);

                WriteAllText(fileName, content, null);
            }
        }
        return fileName;
    }

    public static void AppendAllText(string fileName, string trajectoryContent)
    {
        DirectoryHelpers.EnsureDirectoryForFileExists(fileName);
        File.AppendAllText(fileName, trajectoryContent, Encoding.UTF8);
    }

    public static string? WriteTextToTempFile(string? text, string? extension = null)
    {
        if (!string.IsNullOrEmpty(text))
        {
            var tempFile = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(extension))
            {
                tempFile = $"{tempFile}.{extension}";
            }

            File.WriteAllText(tempFile, text);
            return tempFile;
        }
        return null;
    }

    public static string GetFriendlyLastModified(FileInfo fileInfo)
    {
        var modified = fileInfo.LastWriteTime;
        var modifiedSeconds = (int)((DateTime.Now - modified).TotalSeconds);
        var modifiedMinutes = modifiedSeconds / 60;
        var modifiedHours = modifiedSeconds / 3600;
        var modifiedDays = modifiedSeconds / 86400;

        var formatted =
            modifiedMinutes < 1 ? "just now" :
            modifiedMinutes == 1 ? "1 minute ago" :
            modifiedMinutes < 60 ? $"{modifiedMinutes} minutes ago" :
            modifiedHours == 1 ? "1 hour ago" :
            modifiedHours < 24 ? $"{modifiedHours} hours ago" :
            modifiedDays == 1 ? "1 day ago" :
            modifiedDays < 7 ? $"{modifiedDays} days ago" :
            modified.ToString();

        return formatted;
    }

    public static string GetFriendlySize(FileInfo fileInfo)
    {
        var size = fileInfo.Length;
        var sizeFormatted = size >= 1024 * 1024 * 1024
            ? $"{size / (1024 * 1024 * 1024)} GB"
            : size >= 1024 * 1024
                ? $"{size / (1024 * 1024)} MB"
                : size >= 1024
                    ? $"{size / 1024} KB"
                    : $"{size} bytes";
        return sizeFormatted;
    }

    public static string GetMarkdownLanguage(string extension)
    {
        return extension switch
        {
            ".bat" => "batch",
            ".bmp" => "markdown",
            ".cpp" => "cpp",
            ".cs" => "csharp",
            ".csproj" => "xml",
            ".css" => "css",
            ".docx" => "markdown",
            ".gif" => "markdown",
            ".go" => "go",
            ".html" => "html",
            ".java" => "java",
            ".jpeg" => "markdown",
            ".jpg" => "markdown",
            ".js" => "javascript",
            ".json" => "json",
            ".kt" => "kotlin",
            ".m" => "objective-c",
            ".md" => "markdown",
            ".pdf" => "markdown",
            ".php" => "php",
            ".pl" => "perl",
            ".png" => "markdown",
            ".pptx" => "markdown",
            ".py" => "python",
            ".r" => "r",
            ".rb" => "ruby",
            ".rs" => "rust",
            ".scala" => "scala",
            ".sh" => "bash",
            ".sln" => "xml",
            ".sql" => "sql",
            ".swift" => "swift",
            ".ts" => "typescript",
            ".xml" => "xml",
            ".yaml" => "yaml",
            ".yml" => "yaml",
            _ => "plaintext"
        };
    }

    public static string GenerateUniqueFileNameFromUrl(string url, string saveToFolder)
    {
        DirectoryHelpers.EnsureDirectoryExists(saveToFolder);

        var uri = new Uri(url);
        var path = uri.Host + uri.AbsolutePath + uri.Query;

        var parts = path.Split(_invalidFileNameCharsForWeb, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var check = Path.Combine(saveToFolder, string.Join("-", parts));
        if (!File.Exists(check)) return check;

        while (true)
        {
            var guidPart = Guid.NewGuid().ToString().Substring(0, 8);
            var fileTimePart = DateTime.Now.ToFileTimeUtc().ToString();
            var tryThis = check + "-" + fileTimePart + "-" + guidPart;
            if (!File.Exists(tryThis)) return tryThis;
        }
    }

    public static FileInfo GetProgramAssemblyFileInfo()
    {
        // Assembly.Location always returns empty when the project is built as 
        // a single-file app (which we do when publishing the Dependency package),
        // warning IL3000
        var assembly = ProgramInfo.Assembly;
        string assemblyPath = assembly?.Location ?? string.Empty;
        if (assemblyPath == string.Empty)
        {
            assemblyPath = AppContext.BaseDirectory;
            assemblyPath += assembly?.GetName().Name + ".dll";
        }
        return new FileInfo(assemblyPath);
    }

    private static char[] GetInvalidFileNameCharsForWeb()
    {
        var invalidCharList = Path.GetInvalidFileNameChars().ToList();
        for (char c = (char)0; c < 128; c++)
        {
            if (!char.IsLetterOrDigit(c)) invalidCharList.Add(c);
        }
        return invalidCharList.Distinct().ToArray();
    }

    private static char[] _invalidFileNameCharsForWeb = GetInvalidFileNameCharsForWeb();
}
