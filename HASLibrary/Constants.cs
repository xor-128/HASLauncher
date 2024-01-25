using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HASLibrary
{
    public static class Constants
    {
        public const string LauncherName = "HASLauncher";
        public const string LauncherVersion = "1.0.0";
        public const string ManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";
        public const string JavaManifestUrl = "https://launchermeta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json";

        public const string JavaArguments =
            "-Xmx2G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M ";

        public static readonly string[] DefaultArguments = new[]
        {
            "-Dos.name=Windows 10",
            "-Dos.version=10.0",
            "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump",
            "-Djava.library.path=${natives_directory}", 
            "-Dminecraft.launcher.brand=${launcher_name}",
            "-Dminecraft.launcher.version=${launcher_version}",
            "-Dminecraft.client.jar=${client_jar_path}",
            "-cp",
            "${classpath}"
        };
    }
}
