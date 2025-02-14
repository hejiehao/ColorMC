using ColorMC.Core.Config;
using ColorMC.Core.Helpers;
using ColorMC.Core.Net;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.Loader;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Core.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ColorMC.Core.LaunchPath;

/// <summary>
/// 运行库路径
/// </summary>
public static class LibrariesPath
{
    public const string Name = "libraries";
    public static string BaseDir { get; private set; }
    public static string NativeDir { get; private set; }

    private readonly static Dictionary<string, object> CustomLoader = [];

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="dir">运行路径</param>
    public static void Init(string dir)
    {
        BaseDir = dir + "/" + Name;
        NativeDir = $"{BaseDir}" + $"/native-{SystemInfo.Os}-{SystemInfo.SystemArch}".ToLower();

        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(NativeDir);
    }

    /// <summary>
    /// native路径
    /// </summary>
    /// <param name="version">游戏版本</param>
    /// <returns>路径</returns>
    public static string GetNativeDir(string version)
    {
        string dir = $"{NativeDir}/{version}";
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// 检查游戏运行库
    /// </summary>
    /// <param name="obj">游戏数据</param>
    /// <returns>丢失的库</returns>
    public static async Task<List<DownloadItemObj>> CheckGameLib(this GameArgObj obj, CancellationToken cancel)
    {
        var list = new List<DownloadItemObj>();
        var list1 = await DownloadItemHelper.BuildGameLibs(obj);

        await Parallel.ForEachAsync(list1, cancel, async (item, cancel) =>
        {
            if (cancel.IsCancellationRequested)
                return;

            if (!File.Exists(item.Local))
            {
                list.Add(item);
                return;
            }
            if (ConfigUtils.Config.GameCheck.CheckLibSha1)
            {
                using var stream = new FileStream(item.Local, FileMode.Open, FileAccess.Read,
                    FileShare.Read);
                var sha1 = await HashHelper.GenSha1Async(stream);
                if (!string.IsNullOrWhiteSpace(item.SHA1) && item.SHA1 != sha1)
                {
                    list.Add(item);
                    return;
                }
            }
            if (item.Later != null)
            {
                using var stream = new FileStream(item.Local, FileMode.Open, FileAccess.Read,
                        FileShare.Read);
                item.Later.Invoke(stream);
            }

            return;
        });

        return list;
    }

    /// <summary>
    /// 检查Forge的运行库
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>丢失的库</returns>
    public static async Task<ConcurrentBag<DownloadItemObj>?> CheckForgeLib(this GameSettingObj obj, bool neo, CancellationToken cancel)
    {
        var version1 = VersionPath.GetVersion(obj.Version)!;
        var v2 = CheckHelpers.IsGameVersionV2(version1);
        if (v2)
        {
            GameHelper.ReadyForgeWrapper();
        }

        var forge = neo ?
            VersionPath.GetNeoForgeObj(obj.Version, obj.LoaderVersion!) :
            VersionPath.GetForgeObj(obj.Version, obj.LoaderVersion!);
        if (forge == null)
            return null;

        //forge本体
        var list1 = DownloadItemHelper.BuildForgeLibs(forge, obj.Version, obj.LoaderVersion!, neo);

        var forgeinstall = neo ?
            VersionPath.GetNeoForgeInstallObj(obj.Version, obj.LoaderVersion!) :
            VersionPath.GetForgeInstallObj(obj.Version, obj.LoaderVersion!);
        if (forgeinstall == null && v2)
            return null;

        //forge安装器
        if (forgeinstall != null)
        {
            var list2 = DownloadItemHelper.BuildForgeLibs(forgeinstall, obj.Version,
                obj.LoaderVersion!, neo);
            list1.AddRange(list2);
        }

        var list = new ConcurrentBag<DownloadItemObj>();

        await Parallel.ForEachAsync(list1, cancel, async (item, cancel) =>
        {
            if (cancel.IsCancellationRequested)
                return;

            if (!File.Exists(item.Local))
            {
                list.Add(item);
                return;
            }
            if (item.SHA1 == null)
                return;

            if (!ConfigUtils.Config.GameCheck.CheckLibSha1)
                return;
            using var stream = new FileStream(item.Local, FileMode.Open, FileAccess.ReadWrite,
                FileShare.ReadWrite);
            var sha1 = await HashHelper.GenSha1Async(stream);
            if (item.SHA1 != sha1)
            {
                list.Add(item);
            }
        });

        return list;
    }

    /// <summary>
    /// 检查Fabric的运行库
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>丢失的库</returns>
    public static List<DownloadItemObj>? CheckFabricLib(this GameSettingObj obj, CancellationToken cancel)
    {
        var fabric = VersionPath.GetFabricObj(obj.Version, obj.LoaderVersion!);
        if (fabric == null)
            return null;

        var list = new List<DownloadItemObj>();

        foreach (var item in fabric.libraries)
        {
            if (cancel.IsCancellationRequested)
                break;

            var name = PathHelper.ToPathAndName(item.name);
            string file = $"{BaseDir}/{name.Path}";
            if (!File.Exists(file))
            {
                list.Add(new()
                {
                    Url = UrlHelper.DownloadFabric(item.url + name.Path, BaseClient.Source),
                    Name = name.Name,
                    Local = $"{BaseDir}/{name.Path}"
                });
                continue;
            }
        }

        return list;
    }

    /// <summary>
    /// 检查Quilt的运行库
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>丢失的库</returns>
    public static List<DownloadItemObj>? CheckQuiltLib(this GameSettingObj obj, CancellationToken cancel)
    {
        var quilt = VersionPath.GetQuiltObj(obj.Version, obj.LoaderVersion!);
        if (quilt == null)
            return null;

        var list = new List<DownloadItemObj>();

        foreach (var item in quilt.libraries)
        {
            if (cancel.IsCancellationRequested)
                return null;

            var name = PathHelper.ToPathAndName(item.name);
            string file = $"{BaseDir}/{name.Path}";
            if (!File.Exists(file))
            {
                list.Add(new()
                {
                    Url = UrlHelper.DownloadQuilt(item.url + name.Path, BaseClient.Source),
                    Name = name.Name,
                    Local = $"{BaseDir}/{name.Path}"
                });
                continue;
            }
        }

        return list;
    }

    /// <summary>
    /// 获取游戏核心路径
    /// </summary>
    /// <param name="version">游戏版本</param>
    /// <returns>游戏路径</returns>
    public static string GetGameFile(string version)
    {
        return Path.GetFullPath($"{BaseDir}/net/minecraft/client/{version}/client-{version}.jar");
    }

    /// <summary>
    /// 获取OptiFine路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns></returns>
    public static string GetOptiFineLib(this GameSettingObj obj)
    {
        return GetOptiFineLib(obj.Version, obj.LoaderVersion!);
    }

    /// <summary>
    /// 获取OptiFine路径
    /// </summary>
    /// <param name="mc">游戏版本</param>
    /// <param name="version">optifine版本</param>
    /// <returns></returns>
    public static string GetOptiFineLib(string mc, string version)
    {
        return $"{BaseDir}/optifine/installer/OptiFine-{mc}-{version}.jar";
    }

    /// <summary>
    /// 检测OptiFine是否存在
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool CheckOptifineLib(this GameSettingObj obj)
    {
        return File.Exists(GetOptiFineLib(obj));
    }

    public static async Task<ConcurrentBag<DownloadItemObj>?> DecodeLoaderJar(this GameSettingObj obj, CancellationToken cancel)
    {
        using var zFile = new ZipFile(obj.CustomLoader!.Local);
        using var stream1 = new MemoryStream();
        using var stream2 = new MemoryStream();
        var find1 = false;
        var find2 = false;
        foreach (ZipEntry e in zFile)
        {
            if (e.IsFile && e.Name == "version.json")
            {
                using var stream = zFile.GetInputStream(e);
                await stream.CopyToAsync(stream1, cancel);
                find1 = true;
            }
            else if (e.IsFile && e.Name == "install_profile.json")
            {
                using var stream = zFile.GetInputStream(e);
                await stream.CopyToAsync(stream2, cancel);
                find2 = true;
            }
        }

        if (!find1 || !find2)
        {
            return null;
        }

        var list = new ConcurrentBag<DownloadItemObj>();

        async Task Unpack(ForgeLaunchObj obj1)
        {
            foreach (var item in obj1.libraries)
            {
                if (cancel.IsCancellationRequested)
                {
                    return;
                }
                if (!string.IsNullOrWhiteSpace(item.downloads.artifact.url))
                {
                    string local = Path.GetFullPath($"{BaseDir}/{item.downloads.artifact.path}");
                    {
                        using var read = PathHelper.OpenRead(local);
                        if (read != null)
                        {
                            string sha1 = HashHelper.GenSha1(read);
                            if (sha1 == item.downloads.artifact.sha1)
                            {
                                continue;
                            }
                        }
                    }

                    list.Add(new()
                    {
                        Name = item.name,
                        Local = Path.GetFullPath($"{BaseDir}/{item.downloads.artifact.path}"),
                        SHA1 = item.downloads.artifact.sha1,
                        Url = item.downloads.artifact.url
                    });
                }
                else
                {
                    var item1 = zFile.GetEntry($"maven/{item.downloads.artifact.path}");
                    if (item1 != null)
                    {
                        string local = Path.GetFullPath($"{BaseDir}/{item.downloads.artifact.path}");
                        {
                            using var read = PathHelper.OpenRead(local);
                            if (read != null)
                            {
                                string sha1 = HashHelper.GenSha1(read);
                                if (sha1 == item.downloads.artifact.sha1)
                                {
                                    continue;
                                }
                            }
                        }

                        {
                            using var write = PathHelper.OpenWrite(local);
                            using var stream3 = zFile.GetInputStream(item1);
                            await stream3.CopyToAsync(write, cancel);
                        }
                    }
                }
            }
        }

        try
        {
            byte[] array1 = stream2.ToArray();
            var data = Encoding.UTF8.GetString(array1);
            var obj1 = JsonConvert.DeserializeObject<ForgeLaunchObj>(data)!;

            await Unpack(obj1);

            if (cancel.IsCancellationRequested)
            {
                return null;
            }

            array1 = stream1.ToArray();
            data = Encoding.UTF8.GetString(array1);
            obj1 = JsonConvert.DeserializeObject<ForgeLaunchObj>(data)!;

            await Unpack(obj1);

            if (!CustomLoader.TryAdd(obj.CustomLoader!.Local!, obj1))
            {
                CustomLoader[obj.CustomLoader!.Local!] = obj1;
            }

        }
        catch (Exception e)
        {
            Logs.Error(LanguageHelper.Get("Core.Http.Forge.Error3"), e);
        }

        return list;
    }

    public static List<(string Name, string Local)> GetCustomLoaderLibs(this GameSettingObj obj)
    {
        if (CustomLoader.TryGetValue(obj.CustomLoader!.Local!, out var obj1))
        {
            if (obj1 is ForgeLaunchObj obj2)
            {
                var list = new List<(string, string)>();
                foreach (var item in obj2.libraries)
                {

                    list.Add((item.name, Path.GetFullPath($"{BaseDir}/{item.downloads.artifact.path}")));
                }

                return list;
            }
        }

        return [];
    }

    public static List<string> GetLoaderGameArg(this GameSettingObj obj)
    {
        if (CustomLoader.TryGetValue(obj.CustomLoader!.Local!, out var obj1))
        {
            if (obj1 is ForgeLaunchObj obj2)
            {
                return new(obj2.minecraftArguments.Split(" "));
            }
        }

        return [];
    }

    public static string GetLoaderMainClass(this GameSettingObj obj)
    {
        if (CustomLoader.TryGetValue(obj.CustomLoader!.Local!, out var obj1))
        {
            if (obj1 is ForgeLaunchObj obj2)
            {
                return obj2.mainClass;
            }
        }

        return "";
    }
}
