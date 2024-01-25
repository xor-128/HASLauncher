using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace HASLibrary
{
    public static class VersionManager
    {
        private static class VersionHelper
        {
            private static readonly string[] _currentSystem = { "windows" };
            private static readonly string[] _currentArch = { "x64", "x86" };
            private static string? _path;
            private static JsonNode? _manifestJsonNode;

            internal static void SetPath(string path)
            {
                _path = path;
            }
            internal static JsonNode GetManifest(bool forceRefresh = false)
            {
                if (_manifestJsonNode == null || forceRefresh)
                {
                    _manifestJsonNode = JsonNode.Parse(DownloadManager.GetFileAsString(Constants.ManifestUrl));
                }

                return _manifestJsonNode!;
            }
            internal static JsonNode GetVersion(string version)
            {
                return GetManifest()["versions"]!.AsArray().First(x => x["id"].GetValue<string>() == version);
            }
            internal static List<string> GetAllVersions()
            {
                var manifest = GetManifest();
                return manifest!["versions"]!.AsArray().Select(version => version!["id"]!.GetValue<string>()).ToList();
            }

            internal static List<(string name, string url, string libraryPath)> GetLibraries(string version)
            {
                if (_path == null)
                    throw new ArgumentNullException(nameof(GetLibraries), nameof(version));

                var finalList = new List<(string name, string url, string libraryPath)>();

                var verInfo = GetVersion(version);
                var versionMetadataPath = Path.Combine(_path, "versions", verInfo["id"]!.GetValue<string>(),
                    verInfo["id"]!.GetValue<string>() + ".json");
                var versionMetadata = JsonNode.Parse(File.ReadAllBytes(versionMetadataPath));

                foreach (var library in versionMetadata!["libraries"]!.AsArray())
                {
                    if (library!.AsObject().ContainsKey("rules"))
                    {
                        var libraryRules = library["rules"]!.AsArray();

                        bool invalid = true;

                        foreach (var rule in libraryRules)
                        {
                            if (rule!["action"]!.GetValue<string>() == "allow")
                            {
                                if (!rule.AsObject().ContainsKey("os"))
                                {
                                    invalid = false;
                                    continue;
                                }
                                if (_currentSystem.Contains(rule["os"]!["name"]!.GetValue<string>()))
                                    invalid = false;
                            }
                            else if (rule!["action"]!.GetValue<string>() == "disallow")
                            {
                                if (!rule.AsObject().ContainsKey("os")) continue;
                                if (!_currentSystem.Contains(rule["os"]!["name"]!.GetValue<string>())) continue;

                                invalid = true;
                                break;
                            }
                        }

                        if (invalid)
                        {
                            Console.WriteLine($"{library["name"]!.GetValue<string>()} skipping due rules...");
                            continue;
                        }
                    }

                    if (!library!["downloads"]!.AsObject().ContainsKey("artifact")) continue;

                    var libraryPath = Path.Combine(_path, "libraries",
                        library!["downloads"]!["artifact"]!["path"]!.GetValue<string>());

                    finalList.Add((library!["name"]!.GetValue<string>(),
                        library!["downloads"]!["artifact"]!["url"]!.GetValue<string>(), libraryPath));
                }

                return finalList;
            }

            internal static List<(string name, string url, bool isExctract, JsonObject? extractRules)> GetClassifiers(string version)
            {
                if (_path == null)
                    throw new ArgumentNullException(nameof(GetClassifiers), nameof(version));

                var finalList = new List<(string name, string url, bool isExctract, JsonObject? extractRules)>();

                var verInfo = GetVersion(version);
                var versionMetadataPath = Path.Combine(_path, "versions", verInfo["id"]!.GetValue<string>(),
                    verInfo["id"]!.GetValue<string>() + ".json");
                var versionMetadata = JsonNode.Parse(File.ReadAllBytes(versionMetadataPath));

                var nativesDirectory = Path.Combine(_path, "native_temp", versionMetadata!["downloads"]!["client"]!["sha1"]!.GetValue<string>());

                Directory.CreateDirectory(nativesDirectory);

                if (Directory.EnumerateFileSystemEntries(nativesDirectory).Any())
                    return finalList;

                foreach (var library in versionMetadata!["libraries"]!.AsArray())
                {
                    if (library!.AsObject().ContainsKey("rules"))
                    {
                        var libraryRules = library["rules"]!.AsArray();

                        bool invalid = true;

                        foreach (var rule in libraryRules)
                        {
                            if (rule!["action"]!.GetValue<string>() == "allow")
                            {
                                if (!rule.AsObject().ContainsKey("os"))
                                {
                                    invalid = false;
                                    continue;
                                }
                                if (_currentSystem.Contains(rule["os"]!["name"]!.GetValue<string>()))
                                    invalid = false;
                            }
                            else if (rule!["action"]!.GetValue<string>() == "disallow")
                            {
                                if (!rule.AsObject().ContainsKey("os")) continue;
                                if (!_currentSystem.Contains(rule["os"]!["name"]!.GetValue<string>())) continue;

                                invalid = true;
                                break;
                            }
                        }

                        if (invalid)
                        {
                            Console.WriteLine($"{library["name"]!.GetValue<string>()} skipping due rules...");
                            continue;
                        }
                    }

                    if (!library!["downloads"]!.AsObject().ContainsKey("classifiers"))
                        continue;

                    finalList.Add((library!["name"]!.GetValue<string>(),
                        library!["downloads"]!["classifiers"]!
                            [(library["natives"]!["windows"]!.GetValue<string>())]!["url"]!.GetValue<string>(), true, library.AsObject().ContainsKey("extract") ? library["extract"]!.AsObject() : null));
                }

                return finalList;
            }
        }



        public static bool DownloadVersion(string version, string path)
        {
            VersionHelper.SetPath(path);

            var verInfo = VersionHelper.GetVersion(version);

            var versionMetadataPath = Path.Combine(path, "versions", verInfo["id"]!.GetValue<string>(),
                verInfo["id"]!.GetValue<string>() + ".json");

            if (File.Exists(versionMetadataPath))
            {
                Console.WriteLine($"{version} already has metadata file.");
            }
            else
            {
                DownloadManager.DownloadFile(verInfo["url"]!.GetValue<string>(), versionMetadataPath);
                Console.WriteLine($"{version} metadata downloaded.");
            }

            var versionMetadata = JsonNode.Parse(File.ReadAllBytes(versionMetadataPath));

            var clientFilePath = Path.Combine(path, "versions", verInfo["id"]!.GetValue<string>(),
                verInfo["id"]!.GetValue<string>() + ".jar");

            if (File.Exists(clientFilePath))
            {
                Console.WriteLine($"{version} already has client file.");
            }
            else
            {
                DownloadManager.DownloadFile(versionMetadata!["downloads"]!["client"]!["url"]!.GetValue<string>(), clientFilePath);
                Console.WriteLine($"{version} client file downloaded.");
            }

            var assetIndexPath = Path.Combine(path, "assets", "indexes",
                versionMetadata!["assetIndex"]!["id"]!.GetValue<string>() + ".json");

            if (File.Exists(assetIndexPath))
            {
                Console.WriteLine($"{version} already has asset index.");
            }
            else
            {
                DownloadManager.DownloadFile(versionMetadata["assetIndex"]!["url"]!.GetValue<string>(), assetIndexPath);
                Console.WriteLine($"{version} asset index downloaded.");
            }

            var assetIndex = JsonNode.Parse(File.ReadAllText(assetIndexPath));

            List<(string name, string url, string objectPath)> assetDownloadList = new();

            foreach (var keyPair in assetIndex!["objects"]!.AsObject())
            {
                var assetName = keyPair.Key;
                var asset = keyPair.Value!["hash"]!.GetValue<string>();
                var objectPath = Path.Combine(path, "assets", "objects", asset[..2], asset);

                if (File.Exists(objectPath))
                {
                    Console.WriteLine($"{assetName} already exists.");
                    continue;
                }

                assetDownloadList.Add((assetName, "https://resources.download.minecraft.net/" + asset[..2] + "/" + asset, objectPath));
            }

            Parallel.ForEach(assetDownloadList, new ParallelOptions { MaxDegreeOfParallelism = 500 }, tuple =>
            {
                DownloadManager.DownloadFile(tuple.url, tuple.objectPath);
                Console.WriteLine($"{tuple.name} downloaded.");
            });


            var nativesDirectory = Path.Combine(path, "native_temp",
                versionMetadata["downloads"]!["client"]!["sha1"]!.GetValue<string>());

            Directory.CreateDirectory(nativesDirectory);


            List<(string name, string url, string? libraryPath)> libraryDownloadList = VersionHelper.GetLibraries(version);

            Parallel.ForEach(libraryDownloadList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, tuple =>
            {
                if (!File.Exists(tuple.libraryPath))
                {
                    DownloadManager.DownloadFile(tuple.url, tuple.libraryPath);
                    Console.WriteLine($"{tuple.name} downloaded.");
                }
                else
                {
                    Console.WriteLine($"{tuple.name} already exists.");
                }
            });

            object parallelLock = new object();
            List<(string name, string url, bool isExctract, JsonObject? extractRules)> classifiersDownloadList = VersionHelper.GetClassifiers(version);

            Parallel.ForEach(classifiersDownloadList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, tuple =>
            {
                var fileData = DownloadManager.GetFileAsBytes(tuple.url);

                using (var archive = new ZipArchive(new MemoryStream(fileData), ZipArchiveMode.Read))
                {
                    foreach (var zipArchiveEntry in archive.Entries)
                    {
                        if (tuple.extractRules != null)
                        {
                            if (tuple.extractRules.ContainsKey("exclude"))
                            {
                                if (tuple.extractRules["exclude"]!.AsArray().Any(x =>
                                        zipArchiveEntry.FullName.Contains(x.GetValue<string>())))
                                    continue;
                            }
                            else if (tuple.extractRules.ContainsKey("include"))
                            {
                                if (!tuple.extractRules["include"]!.AsArray().Any(x =>
                                        zipArchiveEntry.FullName.Contains(x.GetValue<string>())))
                                    continue;

                            }
                        }

                        var filePath = Path.Combine(nativesDirectory, zipArchiveEntry.FullName);

                        Directory.CreateDirectory(
                            Path.GetDirectoryName(filePath)!);

                        if (zipArchiveEntry.FullName[^1] != '/')
                        {
                            lock (parallelLock)
                            {
                                zipArchiveEntry.ExtractToFile(filePath, true);
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(filePath);
                        }
                    }
                }

                Console.WriteLine($"{tuple.name} downloaded and extracted.");
            });

            var javaRuntimeDownloadManifest = JsonNode.Parse(DownloadManager.GetFileAsString(Constants.JavaManifestUrl));
            var currentRuntimeName = versionMetadata["javaVersion"]!["component"]!.GetValue<string>();
            var currentRuntimeNode = javaRuntimeDownloadManifest!["windows-x64"]![currentRuntimeName]![0]!["manifest"]!.AsObject();
            var javaRuntimeVersionManifest = JsonNode.Parse(DownloadManager.GetFileAsString(currentRuntimeNode["url"]!.GetValue<string>()));

            var javaRuntimePath = Path.Combine(path, "runtime", currentRuntimeName);

            List<(string name, string url, string entryPath)> javaDownloadList = new();

            foreach (var fileEntry in javaRuntimeVersionManifest!["files"]!.AsObject())
            {
                var name = fileEntry.Key;
                var value = fileEntry.Value;
                if (value!["type"]!.GetValue<string>() != "file") continue;

                var entryPath = Path.Combine(javaRuntimePath, name);

                if (File.Exists(entryPath))
                {
                    Console.WriteLine($"{name} already exists in runtime folder...");
                    continue;
                }

                javaDownloadList.Add((name, value!["downloads"]!["raw"]!["url"]!.GetValue<string>(), entryPath));

            }

            Parallel.ForEach(javaDownloadList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, tuple =>
            {
                DownloadManager.DownloadFile(tuple.url, tuple.entryPath);
                Console.WriteLine($"{tuple.name} downloaded.");
            });

            return true;
        }

        public static bool StartGame(string version, string path, string username)
        {
            VersionHelper.SetPath(path);

            var verInfo = VersionHelper.GetVersion(version);

            var versionMetadataPath = Path.Combine(path, "versions", verInfo["id"]!.GetValue<string>(),
                verInfo["id"]!.GetValue<string>() + ".json");

            if (!File.Exists(versionMetadataPath))
            {
                Console.WriteLine($"{version} not exists in folder.");
                return false;
            }


            var versionMetadata = JsonNode.Parse(File.ReadAllBytes(versionMetadataPath))!.AsObject();

            var currentRuntimeName = versionMetadata!["javaVersion"]!["component"]!.GetValue<string>();
            var javaRuntimePath = Path.Combine(path, "runtime", currentRuntimeName);
            var nativesDirectory = Path.Combine(path, "native_temp",
                versionMetadata["downloads"]!["client"]!["sha1"]!.GetValue<string>());
            var clientFilePath = Path.Combine(path, "versions", verInfo["id"]!.GetValue<string>(),
                verInfo["id"]!.GetValue<string>() + ".jar");
            if (!Directory.Exists(nativesDirectory))
            {
                Directory.CreateDirectory(nativesDirectory);
            }

            var classPaths = "";

            foreach (var (_, _, libraryPath) in VersionHelper.GetLibraries(version))
            {
                if (!classPaths.Contains(libraryPath.Replace("/", "\\")))
                    classPaths += libraryPath.Replace("/", "\\") + ";";
            }

            classPaths += clientFilePath;

            var arguments = "";

            Dictionary<string, string> argumentMap = new Dictionary<string, string>
            {
                {"${auth_player_name}", username},
                {"${version_name}", version},
                {"${game_directory}", path},
                {"${assets_root}", Path.Combine(path, "assets")},
                {"${client_jar_path}", Path.Combine(path, "versions", verInfo["id"]!.GetValue<string>(), verInfo["id"]!.GetValue<string>() + ".jar")},
                {"${game_assets}", Path.Combine(path, "assets", "virtual", versionMetadata["assetIndex"]["id"].GetValue<string>())},
                {"${assets_index_name}", versionMetadata["assetIndex"]["id"].GetValue<string>()},
                {"${auth_uuid}", Guid.NewGuid().ToString().ToLower().Replace("-", "")},
                {"${auth_access_token}", "null"},
                {"${auth_session}", "token:null"},
                {"${clientid}", Guid.NewGuid().ToString().ToLower().Replace("-", "")},
                {"${auth_xuid}", Random.Shared.NextInt64(0, Int64.MaxValue).ToString()},
                {"${user_type}", "msa"},
                {"${version_type}", versionMetadata["type"].GetValue<string>()},
                {"${natives_directory}", nativesDirectory},
                {"${launcher_name}", Constants.LauncherName},
                {"${launcher_version}", Constants.LauncherVersion},
                {"${classpath}", classPaths},
            };

            string ParseArgument(string argument, bool disableEscaping = false)
            {
                string final = argument;

                foreach (var key in argumentMap.Keys.Where(argument.Contains))
                {
                    final = final.Replace(key, argumentMap[key]);
                }

                if (final.Contains(' ') && !disableEscaping)
                    final = $"\"{final}\"";

                return final;
            }

            if (versionMetadata.ContainsKey("arguments") && versionMetadata["arguments"]!.AsObject().ContainsKey("jvm"))
            {
                foreach (var argument in versionMetadata["arguments"]!["jvm"]!.AsArray())
                {
                    if (argument is JsonObject)
                    {
                        if (argument.AsObject().ContainsKey("rules"))
                        {
                            bool invalid = true;

                            foreach (var rule in argument["rules"]!.AsArray())
                            {
                                if (rule!["action"]!.GetValue<string>() == "allow")
                                {
                                    if (rule.AsObject().ContainsKey("os"))
                                    {
                                        if (rule["os"]!.AsObject().ContainsKey("name") &&
                                            rule["os"]!["name"]!.GetValue<string>() == "windows")
                                            invalid = false;
                                        if (rule["os"]!.AsObject().ContainsKey("arch") &&
                                            rule["os"]!["arch"]!.GetValue<string>() == "x64")
                                            invalid = false;
                                    }
                                }
                                else if (rule!["action"]!.GetValue<string>() == "disallow")
                                {
                                    if (rule.AsObject().ContainsKey("os"))
                                    {
                                        if (rule["os"]!["name"]!.GetValue<string>() != "windows") continue;

                                        invalid = true;
                                        break;
                                    }
                                }
                            }

                            if (invalid)
                            {
                                Console.WriteLine($"{argument["value"]} skipping due rules...");
                                continue;
                            }


                        }

                        if (argument["value"] is JsonArray)
                        {
                            foreach (var arg in argument["value"]!.AsArray())
                            {
                                arguments += ParseArgument(arg!.GetValue<string>()) + " ";
                            }
                        }
                        else if (argument["value"] is JsonValue)
                        {
                            arguments += ParseArgument(argument["value"]!.GetValue<string>()) + " ";
                        }

                    }
                    else if (argument is JsonValue)
                    {
                        arguments += ParseArgument(argument.GetValue<string>()) + " ";
                    }
                }
            }

            if (!arguments.Contains("-cp"))
            {
                arguments = Constants.DefaultArguments.Aggregate(arguments, (current, defaultArgument) => current + (ParseArgument(defaultArgument) + " "));
            }

            arguments += Constants.JavaArguments + versionMetadata["mainClass"]!.GetValue<string>() + " ";

            if (versionMetadata.ContainsKey("arguments") && versionMetadata["arguments"]!.AsObject().ContainsKey("game"))
            {
                foreach (var argument in versionMetadata["arguments"]!["game"]!.AsArray())
                {
                    if (argument is JsonObject)
                    {
                        if (argument.AsObject().ContainsKey("rules"))
                        {
                            bool invalid = true;

                            foreach (var rule in argument["rules"]!.AsArray())
                            {
                                if (rule!["action"]!.GetValue<string>() == "allow")
                                {
                                    if (rule.AsObject().ContainsKey("os"))
                                    {
                                        if (rule["os"]!.AsObject().ContainsKey("name") &&
                                            rule["os"]!["name"]!.GetValue<string>() == "windows")
                                            invalid = false;
                                        if (rule["os"]!.AsObject().ContainsKey("arch") &&
                                            (rule["os"]!["arch"]!.GetValue<string>() == "x64" ||
                                             rule["os"]!["arch"]!.GetValue<string>() == "x86"))
                                            invalid = false;
                                    }
                                    else if (rule.AsObject().ContainsKey("features"))
                                    {
                                        if (rule["features"]!.AsObject().ContainsKey("is_quick_play_realms") &&
                                            rule["features"]!["is_quick_play_realms"]!.GetValue<bool>())
                                        {
                                            invalid = true;
                                            break;
                                        }

                                        if (rule["features"]!.AsObject()
                                                .ContainsKey("is_quick_play_singleplayer") &&
                                            rule["features"]!["is_quick_play_singleplayer"]!.GetValue<bool>())
                                        {
                                            invalid = true;
                                            break;
                                        }

                                        if (rule["features"]!.AsObject().ContainsKey("is_quick_play_multiplayer") &&
                                            rule["features"]!["is_quick_play_multiplayer"]!.GetValue<bool>())
                                        {
                                            invalid = true;
                                            break;
                                        }

                                        if (rule["features"]!.AsObject().ContainsKey("has_quick_plays_support") &&
                                            rule["features"]!["has_quick_plays_support"]!.GetValue<bool>())
                                        {
                                            invalid = true;
                                            break;
                                        }

                                        if (rule["features"]!.AsObject().ContainsKey("has_quick_plays_support") &&
                                            rule["features"]!["has_custom_resolution"]!.GetValue<bool>())
                                        {
                                            invalid = true;
                                            break;
                                        }

                                        if (rule["features"]!.AsObject().ContainsKey("has_quick_plays_support") &&
                                            rule["features"]!["is_demo_user"]!.GetValue<bool>())
                                        {
                                            invalid = true;
                                            break;
                                        }
                                    }
                                }
                                else if (rule!["action"]!.GetValue<string>() == "disallow")
                                {
                                    if (rule.AsObject().ContainsKey("os"))
                                    {
                                        if (rule["os"]!["name"]!.GetValue<string>() != "windows") continue;

                                        invalid = true;
                                        break;
                                    }
                                }
                            }

                            if (invalid)
                            {
                                Console.WriteLine($"{argument["value"]} skipping due rules...");
                                continue;
                            }


                            if (argument["value"] is JsonArray)
                            {
                                foreach (var arg in argument["value"]!.AsArray())
                                {
                                    arguments += ParseArgument(arg!.GetValue<string>()) + " ";
                                }
                            }
                            else if (argument["value"] is JsonValue)
                            {
                                arguments += ParseArgument(argument["value"]!.GetValue<string>()) + " ";
                            }

                        }
                    }
                    else if (argument is JsonValue)
                    {
                        arguments += ParseArgument(argument.GetValue<string>()) + " ";
                    }
                }
            }

            if (versionMetadata.ContainsKey("minecraftArguments"))
            {
                arguments += ParseArgument(versionMetadata["minecraftArguments"]!.GetValue<string>(), true) + " ";
            }

            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = path,
                FileName = Path.Combine(javaRuntimePath, "bin/javaw.exe"),
                Arguments = arguments
            });

            return true;
        }

        public enum VersionListType
        {
            All,
            Release,
            Snapshot,
            OldBeta,
            OldAlpha
        }

        public static List<string> GetAllVersions(VersionListType versionList = VersionListType.All, bool forceUpdate = false)
        {
            switch (versionList)
            {
                case VersionListType.All:
                    {
                        if (forceUpdate)
                            VersionHelper.GetManifest(true);
                        return VersionHelper.GetAllVersions();
                    }
                case VersionListType.Release:
                case VersionListType.Snapshot:
                case VersionListType.OldBeta:
                case VersionListType.OldAlpha:
                    {
                        var typeNames = new Dictionary<VersionListType, string>
                        {
                            { VersionListType.Release, "release"},
                            { VersionListType.Snapshot, "snapshot"},
                            { VersionListType.OldBeta, "old_beta"},
                            { VersionListType.OldAlpha, "old_alpha"},
                        };

                        var manifest = VersionHelper.GetManifest(forceUpdate);
                        var versions = manifest["versions"]!.AsArray();

                        return versions.Where(x => x!["type"]!.GetValue<string>() == typeNames[versionList])
                            .Select(x => x!["id"]!.GetValue<string>()).ToList();
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(versionList), versionList, null);
            }
        }
    }
}
