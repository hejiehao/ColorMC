using ColorMC.Core.Config;
using ColorMC.Core.Downloader;
using ColorMC.Core.Helpers;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.CurseForge;
using ColorMC.Core.Objs.Modrinth;
using ColorMC.Core.Objs.OtherLaunch;
using ColorMC.Core.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System.Text;

namespace ColorMC.Core.LaunchPath;

/// <summary>
/// 游戏实例路径
/// </summary>
public static class InstancesPath
{
    public const string Name = "instances";
    public const string Name1 = "game.json";
    public const string Name2 = ".minecraft";
    public const string Name3 = "modinfo.json";
    public const string Name4 = "modpack.json";
    public const string Name5 = "options.txt";
    public const string Name6 = "servers.dat";
    public const string Name7 = "screenshots";
    public const string Name8 = "resourcepacks";
    public const string Name9 = "shaderpacks";
    public const string Name10 = "icon.png";
    public const string Name11 = "mods";
    public const string Name12 = "saves";
    public const string Name13 = "config";
    public const string Name14 = "modfileinfo.json";
    public const string Name15 = "logs";
    public const string Name16 = "schematics";
    public const string Name17 = "remove";
    public const string Name18 = "backup";
    public const string Name19 = "server.json";
    public const string Name20 = "server.old.json";
    public const string Name21 = "launch.json";
    public const string Name22 = "log4j-rce-patch.xml";
    public const string Name23 = "temp";
    public const string Name24 = "cache";

    public const string DefaultGroup = " ";

    /// <summary>
    /// 游戏实例列表
    /// </summary>
    private readonly static Dictionary<string, GameSettingObj> s_installGames = [];
    /// <summary>
    /// 游戏实例组
    /// </summary>
    private readonly static Dictionary<string, List<GameSettingObj>> s_gameGroups = [];

    /// <summary>
    /// 基础路径
    /// </summary>
    public static string BaseDir { get; private set; }

    /// <summary>
    /// 获取所有游戏实例
    /// </summary>
    public static List<GameSettingObj> Games
    {
        get
        {
            return new(s_installGames.Values);
        }
    }

    /// <summary>
    /// 获取所有游戏实例组
    /// </summary>
    public static Dictionary<string, List<GameSettingObj>> Groups => new(s_gameGroups);

    /// <summary>
    /// 是否没有游戏实例
    /// </summary>
    public static bool IsNotGame => s_installGames.Count == 0;

    /// <summary>
    /// 添加游戏实例到组
    /// </summary>
    /// <param name="obj">游戏实例</param>
    private static void AddToGroup(this GameSettingObj obj)
    {
        while (string.IsNullOrWhiteSpace(obj.UUID)
            || s_installGames.ContainsKey(obj.UUID))
        {
            obj.UUID = Guid.NewGuid().ToString();
            obj.Save();
        }
        s_installGames.Add(obj.UUID, obj);

        if (string.IsNullOrWhiteSpace(obj.GroupName))
        {
            s_gameGroups[DefaultGroup].Add(obj);
        }
        else
        {
            if (s_gameGroups.TryGetValue(obj.GroupName, out var group))
            {
                group.Add(obj);
            }
            else
            {
                var list = new List<GameSettingObj>()
                {
                    obj
                };
                s_gameGroups.Add(obj.GroupName, list);
            }
        }
    }

    /// <summary>
    /// 从组中删除游戏实例
    /// </summary>
    /// <param name="obj"></param>
    private static void RemoveFromGroup(this GameSettingObj obj)
    {
        s_installGames.Remove(obj.UUID);

        if (string.IsNullOrEmpty(obj.GroupName) || obj.GroupName == DefaultGroup)
        {
            s_gameGroups[DefaultGroup].Remove(obj);
        }
        else if (s_gameGroups.TryGetValue(obj.GroupName, out var group))
        {
            group.Remove(obj);

            if (group.Count == 0)
            {
                s_gameGroups.Remove(obj.GroupName);
            }
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="dir">运行路径</param>
    public static void Init(string dir)
    {
        BaseDir = Path.GetFullPath(dir + "/" + Name);

        Directory.CreateDirectory(BaseDir);

        s_gameGroups.Add(DefaultGroup, []);

        var list = Directory.GetDirectories(BaseDir);
        foreach (var item in list)
        {
            var file = Path.GetFullPath(item + "/" + Name1);
            GameSettingObj? game = null;
            if (!File.Exists(file))
            {
                var file1 = Path.GetFullPath(item + "/" + "mmc-pack.json");
                var file2 = Path.GetFullPath(item + "/" + "instance.cfg");
                if (File.Exists(file1) && File.Exists(file2))
                {
                    var mmc = JsonConvert.DeserializeObject<MMCObj>(PathHelper.ReadText(file1)!);
                    if (mmc == null)
                        break;

                    var mmc1 = PathHelper.ReadText(file2)!;
                    game = GameHelper.ToColorMC(mmc, mmc1, out var icon);
                    game.Icon = icon + ".png";
                }
            }
            else
            {
                var data1 = PathHelper.ReadText(file)!;
                game = JsonConvert.DeserializeObject<GameSettingObj>(data1);
            }
            if (game != null)
            {
                var path = Path.GetFileName(item);
                if (path != game.DirName)
                {
                    game.DirName = path;
                    game.Save();
                }
                game.ReadModInfo();
                game.ReadLaunchData();
                game.AddToGroup();
            }
        }
    }

    /// <summary>
    /// 获取游戏实例
    /// </summary>
    /// <param name="uuid">实例UUID</param>
    /// <returns>游戏实例</returns>
    public static GameSettingObj? GetGame(string? uuid)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            return null;

        if (s_installGames.TryGetValue(uuid, out var item))
        {
            return item;
        }

        return null;
    }

    /// <summary>
    /// 从游戏名字取游戏实例
    /// </summary>
    /// <param name="name">游戏名字</param>
    /// <returns>游戏实例</returns>
    public static GameSettingObj? GetGameByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return s_installGames.Values.FirstOrDefault(a => a.Name == name);
    }

    /// <summary>
    /// 保存游戏实例
    /// </summary>
    /// <param name="obj">游戏实例</param>
    public static void Save(this GameSettingObj obj)
    {
        ConfigSave.AddItem(new()
        {
            Name = $"game-{obj.UUID}",
            Local = obj.GetGameJsonFile(),
            Obj = obj
        });
    }

    /// <summary>
    /// 获取游戏实例基础路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetBasePath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}");
    }

    /// <summary>
    /// 获取游戏实例游戏路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetGamePath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}");
    }

    /// <summary>
    /// 获取游戏实例储存文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetGameJsonFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name1}");
    }

    /// <summary>
    /// 获取游戏实例mod信息文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetModJsonFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name3}");
    }

    /// <summary>
    /// 获取游戏实例整合包文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetModPackJsonFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name4}");
    }

    /// <summary>
    /// 获取游戏实例视频选项文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetOptionsFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name5}");
    }

    /// <summary>
    /// 获取游戏实例服务器储存文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetServersFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name6}");
    }

    /// <summary>
    /// 获取游戏实例截图路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetScreenshotsPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name7}/");
    }

    /// <summary>
    /// 获取游戏实例资源包路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetResourcepacksPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name8}/");
    }

    /// <summary>
    /// 获取游戏实例光影包路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetShaderpacksPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name9}/");
    }

    /// <summary>
    /// 获取游戏实例图标文件路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetIconFile(this GameSettingObj obj)
    {
        if (string.IsNullOrWhiteSpace(obj.Icon))
        {
            obj.Icon = Name10;
            obj.Save();
        }
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{obj.Icon}");
    }

    /// <summary>
    /// 获取游戏实例mod文件路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetModsPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name11}/");
    }

    /// <summary>
    /// 获取游戏实例世界路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetSavesPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name12}/");
    }

    /// <summary>
    /// 获取结构文件路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetSchematicsPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name16}/");
    }

    /// <summary>
    /// 世界备份
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetWorldBackupPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name18}/");
    }

    /// <summary>
    /// 获取游戏实例配置文件路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetConfigPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name13}/");
    }

    /// <summary>
    /// 获取游戏实例mod数据文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetModInfoJsonFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name14}");
    }

    /// <summary>
    /// 获取游戏实例最后日志路径
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetLogPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name2}/{Name15}");
    }

    /// <summary>
    /// 获取删除世界的文件夹
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>路径</returns>
    public static string GetRemoveWorldPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name17}/{Name12}");
    }

    /// <summary>
    /// 获取服务器实例文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetServerPackFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name19}");
    }

    /// <summary>
    /// 获取旧服务器实例文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>文件路径</returns>
    public static string GetServerPackOldFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name20}");
    }

    /// <summary>
    /// 获取启动数据
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns></returns>
    public static string GetLaunchFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name21}");
    }

    /// <summary>
    /// 获取安全Log4j文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns></returns>
    public static string GetLog4jFile(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name22}");
    }

    /// <summary>
    /// 游戏临时路径
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string GetGameTempPath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name23}");
    }

    /// <summary>
    /// 游戏缓存路径
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string GetGameCachePath(this GameSettingObj obj)
    {
        return Path.GetFullPath($"{BaseDir}/{obj.DirName}/{Name24}");
    }

    /// <summary>
    /// 新建游戏版本
    /// </summary>
    /// <param name="game">游戏实例</param>
    /// <returns>结果</returns>
    public static async Task<GameSettingObj?> CreateGame(this GameSettingObj game)
    {
        var value = s_installGames.Values.FirstOrDefault(item => item.DirName == game.Name);
        if (value != null)
        {
            if (ColorMCCore.GameOverwirte == null)
            {
                return null;
            }

            if (await ColorMCCore.GameOverwirte.Invoke(game) == false)
            {
                return null;
            }

            if (s_installGames.Remove(value.UUID, out var temp))
            {
                if (!await Remove(temp))
                {
                    return null;
                }
            }
        }
        game.DirName = game.Name;

        var dir = game.GetBasePath();
        if (!await PathHelper.DeleteFiles(dir))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(game.GetGamePath());
        }
        catch (Exception e)
        {
            Logs.Error(string.Format(LanguageHelper.Get("Core.Game.Error15"), game.Name), e);
            return null;
        }

        game.Mods ??= [];
        game.LaunchData ??= new()
        {
            AddTime = DateTime.Now,
            LastPlay = new()
        };

        game.Save();
        game.SaveLaunchData();
        game.AddToGroup();

        return game;
    }

    /// <summary>
    /// 重读配置
    /// </summary>
    /// <param name="game">游戏实例</param>
    /// <returns>重载后的实例</returns>
    public static GameSettingObj Reload(this GameSettingObj game)
    {
        var data1 = PathHelper.ReadText(game.GetGameJsonFile())!;
        var obj = JsonConvert.DeserializeObject<GameSettingObj>(data1);
        if (obj != null)
        {
            game.RemoveFromGroup();
            obj.Name = game.Name;
            obj.UUID = game.UUID;
            obj.DirName = game.DirName;
            obj.ReadModInfo(false);
            obj.ReadLaunchData();
            obj.AddToGroup();
            obj.Save();
        }
        return obj ?? game;
    }

    /// <summary>
    /// 新建游戏组
    /// </summary>
    /// <param name="name">组名字</param>
    /// <returns>结果</returns>
    public static bool AddGroup(string name)
    {
        if (s_gameGroups.ContainsKey(name))
        {
            return false;
        }

        s_gameGroups.Add(name, []);

        return true;
    }

    /// <summary>
    /// 移动游戏实例到组
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="now">组名</param>
    public static void MoveGameGroup(this GameSettingObj obj, string? now)
    {
        var group = obj.GroupName;
        if (string.IsNullOrWhiteSpace(group))
        {
            s_gameGroups[DefaultGroup].Remove(obj);
        }
        else
        {
            var list = s_gameGroups[group];
            list.Remove(obj);

            if (list.Count == 0)
            {
                s_gameGroups.Remove(group);
            }
        }

        if (string.IsNullOrWhiteSpace(now))
        {
            s_gameGroups[DefaultGroup].Add(obj);
        }
        else
        {
            AddGroup(now);
            s_gameGroups[now].Add(obj);
        }

        obj.GroupName = now!;
        obj.Save();
    }

    /// <summary>
    /// 复制游戏实例存储
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>游戏实例</returns>
    public static GameSettingObj CopyObj(this GameSettingObj obj)
    {
        return new()
        {
            Name = obj.Name,
            GroupName = obj.GroupName,
            DirName = obj.DirName,
            Version = obj.Version,
            ModPack = obj.ModPack,
            Loader = obj.Loader,
            LoaderVersion = obj.LoaderVersion,
            JvmArg = obj.JvmArg?.Copy(),
            JvmName = obj.JvmName,
            JvmLocal = obj.JvmLocal,
            Window = obj.Window?.Copy(),
            StartServer = obj.StartServer?.Copy(),
            ProxyHost = obj.ProxyHost?.Copy(),
            AdvanceJvm = obj.AdvanceJvm?.Copy(),
            ServerUrl = obj.ServerUrl,
            Mods = new(obj.Mods),
            FID = obj.FID,
            PID = obj.PID,
            ModPackType = obj.ModPackType,
            GameType = obj.GameType,
            Icon = obj.Icon
        };
    }

    /// <summary>
    /// 复制游戏实例存储
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>游戏实例</returns>
    public static void CopyObj(this GameSettingObj obj, GameSettingObj now)
    {
        now.Version = obj.Version;
        now.Loader = obj.Loader;
        now.LoaderVersion = obj.LoaderVersion;

        if (obj.JvmArg != null)
        {
            now.JvmArg = obj.JvmArg;
        }
        if (obj.Window != null)
        {
            now.Window = obj.Window;
        }
        if (obj.StartServer != null)
        {
            now.StartServer = obj.StartServer;
        }
        if (obj.AdvanceJvm != null)
        {
            now.AdvanceJvm = obj.AdvanceJvm;
        }

        now.Save();
    }

    /// <summary>
    /// 复制实例
    /// </summary>
    /// <param name="obj">原始实例</param>
    /// <param name="name">新的名字</param>
    /// <returns>复制结果</returns>
    public static async Task<GameSettingObj?> Copy(this GameSettingObj obj, string name)
    {
        var obj1 = obj.CopyObj();
        obj1.Name = name;
        obj1 = await CreateGame(obj1);
        if (obj1 != null)
        {
            await PathHelper.CopyDir(GetGamePath(obj), GetGamePath(obj1));
            string file = obj.GetIconFile();
            if (File.Exists(file))
            {
                PathHelper.CopyFile(file, obj1.GetIconFile());
            }
            file = obj.GetModJsonFile();
            if (File.Exists(file))
            {
                PathHelper.CopyFile(file, obj1.GetModJsonFile());
            }
            file = obj.GetModInfoJsonFile();
            if (File.Exists(file))
            {
                PathHelper.CopyFile(file, obj1.GetModInfoJsonFile());
            }

            return obj1;
        }

        return null;
    }

    /// <summary>
    /// 保存游戏实例mod数据
    /// </summary>
    /// <param name="obj">游戏实例</param>
    public static void SaveModInfo(this GameSettingObj obj)
    {
        if (obj.Mods == null)
            return;

        ConfigSave.AddItem(new()
        {
            Name = $"game-mod-{obj.Name}",
            Local = obj.GetModInfoJsonFile(),
            Obj = obj.Mods
        });
    }

    /// <summary>
    /// 保存游戏启动数据
    /// </summary>
    /// <param name="obj">游戏实例</param>
    public static void SaveLaunchData(this GameSettingObj obj)
    {
        if (obj.LaunchData == null)
            return;

        ConfigSave.AddItem(new()
        {
            Name = $"game-launch-{obj.Name}",
            Local = obj.GetLaunchFile(),
            Obj = obj.LaunchData
        });
    }

    /// <summary>
    /// 读取游戏实例mod数据
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="delete">是否删除不存在的mod数据</param>
    public static void ReadModInfo(this GameSettingObj obj, bool delete = true)
    {
        string file = obj.GetModInfoJsonFile();
        if (!File.Exists(file))
        {
            obj.Mods = [];
            return;
        }

        try
        {
            var temp = PathHelper.ReadText(file)!;
            var res = JsonConvert.DeserializeObject<Dictionary<string, ModInfoObj>>(temp);
            if (res == null)
            {
                obj.Mods = [];
            }
            else
            {
                obj.Mods = res;
            }

            if (obj.ModPack || !delete)
            {
                return;
            }

            var list = PathHelper.GetAllFile(obj.GetModsPath());
            var remove = new List<string>();
            foreach (var item in obj.Mods)
            {
                bool find = false;
                foreach (var item1 in list)
                {
                    if (item.Value.File == item1.Name)
                    {
                        find = true;
                        break;
                    }
                }

                if (!find)
                {
                    remove.Add(item.Key);
                }
            }

            if (remove.Count != 0)
            {
                remove.ForEach(item => obj.Mods.Remove(item));
                obj.SaveModInfo();
            }
        }
        catch (Exception e)
        {
            Logs.Error(LanguageHelper.Get("Core.Game.Error8"), e);
            obj.Mods = [];
            return;
        }
    }

    /// <summary>
    /// 读取游戏运行时间
    /// </summary>
    /// <param name="obj">游戏实例</param>
    public static void ReadLaunchData(this GameSettingObj obj)
    {
        string file = obj.GetLaunchFile();
        if (!File.Exists(file))
        {
            obj.LaunchData = new()
            {
                LastPlay = new()
            };
            return;
        }

        try
        {
            var res = JsonConvert.DeserializeObject<LaunchDataObj>(
            PathHelper.ReadText(file)!);
            obj.LaunchData = res ?? new() { LastPlay = new() };
        }
        catch (Exception e)
        {
            Logs.Error(LanguageHelper.Get("Core.Game.Error9"), e);
            obj.LaunchData = new()
            {
                LastPlay = new()
            };
            return;
        }
    }

    /// <summary>
    /// 删除游戏实例
    /// </summary>
    /// <param name="obj">游戏实例</param>
    public static Task<bool> Remove(this GameSettingObj obj)
    {
        obj.RemoveFromGroup();
        PathHelper.Delete(obj.GetGameJsonFile());
        return PathHelper.DeleteFiles(obj.GetBasePath());
    }

    /// <summary>
    /// 导入整合包
    /// </summary>
    /// <param name="dir">压缩包路径</param>
    /// <param name="type">类型</param>
    /// <param name="type">名字</param>
    /// <param name="type">群组</param>
    public static async Task<(bool, GameSettingObj?)> InstallZip(string dir, PackType type, string? name, string? group)
    {
        GameSettingObj? game = null;
        bool import = false;
        Stream? stream4 = null;
        try
        {
            stream4 = PathHelper.OpenRead(dir);
            switch (type)
            {
                //ColorMC格式
                case PackType.ColorMC:
                    {
                        ColorMCCore.PackState?.Invoke(CoreRunState.Read);
                        using ZipFile zFile = new(stream4);
                        using var stream1 = new MemoryStream();
                        bool find = false;
                        foreach (ZipEntry e in zFile)
                        {
                            if (e.IsFile && e.Name == "game.json")
                            {
                                using var stream = zFile.GetInputStream(e);
                                await stream.CopyToAsync(stream1);
                                find = true;
                                break;
                            }
                        }

                        if (!find)
                            break;

                        game = JsonConvert.DeserializeObject<GameSettingObj>
                            (Encoding.UTF8.GetString(stream1.ToArray()));

                        if (game == null)
                            break;

                        if (s_installGames.ContainsKey(game.Name))
                        {
                            break;
                        }

                        foreach (ZipEntry e in zFile)
                        {
                            if (e.IsFile)
                            {
                                using var stream = zFile.GetInputStream(e);
                                var path = game.GetBasePath();
                                string file = Path.GetFullPath(path + '\\' + e.Name);
                                FileInfo info2 = new(file);
                                info2.Directory?.Create();
                                using FileStream stream2 = new(file, FileMode.Create,
                                    FileAccess.ReadWrite, FileShare.ReadWrite);
                                await stream.CopyToAsync(stream2);
                            }
                        }

                        game.AddToGroup();

                        ColorMCCore.PackState?.Invoke(CoreRunState.End);
                        import = true;
                        break;
                    }
                //Curseforge压缩包
                case PackType.CurseForge:
                    (import, game) = await ModPackHelper.DownloadCurseForgeModPack(dir, name, group);

                    ColorMCCore.PackState?.Invoke(CoreRunState.End);
                    break;
                //Modrinth压缩包
                case PackType.Modrinth:
                    (import, game) = await ModPackHelper.DownloadModrinthModPack(dir, name, group);

                    ColorMCCore.PackState?.Invoke(CoreRunState.End);
                    break;
                //MMC压缩包
                case PackType.MMC:
                    {
                        ColorMCCore.PackState?.Invoke(CoreRunState.Read);
                        using ZipFile zFile = new(stream4);
                        using var stream1 = new MemoryStream();
                        using var stream2 = new MemoryStream();
                        bool find = false;
                        bool find1 = false;
                        string path = "";
                        foreach (ZipEntry e in zFile)
                        {
                            if (e.IsFile && !find && e.Name.EndsWith("mmc-pack.json"))
                            {
                                using var stream = zFile.GetInputStream(e);
                                await stream.CopyToAsync(stream1);
                                path = e.Name[..^Path.GetFileName(e.Name).Length];
                                find = true;
                            }

                            if (e.IsFile && !find1 && e.Name.EndsWith("instance.cfg"))
                            {
                                using var stream = zFile.GetInputStream(e);
                                await stream.CopyToAsync(stream2);
                                find1 = true;
                            }

                            if (find && find1)
                                break;
                        }

                        if (!find || !find1)
                            break;

                        var mmc = JsonConvert.DeserializeObject<MMCObj>
                            (Encoding.UTF8.GetString(stream1.ToArray()));
                        if (mmc == null)
                            break;

                        var mmc1 = Encoding.UTF8.GetString(stream2.ToArray());

                        game = mmc.ToColorMC(mmc1, out var icon);

                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            game.Name = name;
                        }
                        if (!string.IsNullOrWhiteSpace(group))
                        {
                            game.GroupName = group;
                        }
                        if (string.IsNullOrWhiteSpace(game.Name))
                        {
                            game.Name = new FileInfo(dir).Name;
                        }
                        if (!string.IsNullOrWhiteSpace(icon))
                        {
                            game.Icon = icon + ".png";
                        }
                        game = await CreateGame(game);

                        if (game == null)
                            break;

                        foreach (ZipEntry e in zFile)
                        {
                            if (e.IsFile && e.Name.StartsWith(path))
                            {
                                using var stream = zFile.GetInputStream(e);
                                string file = Path.GetFullPath(game.GetBasePath() + "/" +
                                    e.Name[path.Length..]);
                                FileInfo info2 = new(file);
                                info2.Directory?.Create();
                                using FileStream stream3 = new(file, FileMode.Create,
                                    FileAccess.ReadWrite, FileShare.ReadWrite);
                                await stream.CopyToAsync(stream3);
                            }
                        }

                        ColorMCCore.PackState?.Invoke(CoreRunState.End);
                        import = true;
                        break;
                    }
                //HMCL压缩包
                case PackType.HMCL:
                    {
                        ColorMCCore.PackState?.Invoke(CoreRunState.Read);
                        using ZipFile zFile = new(stream4);
                        using var stream1 = new MemoryStream();
                        using var stream2 = new MemoryStream();
                        bool find = false;
                        bool find1 = false;
                        foreach (ZipEntry e in zFile)
                        {
                            if (e.IsFile && e.Name == "mcbbs.packmeta")
                            {
                                using var stream = zFile.GetInputStream(e);
                                await stream.CopyToAsync(stream1);
                                find = true;
                            }

                            if (e.IsFile && e.Name == "manifest.json")
                            {
                                using var stream = zFile.GetInputStream(e);
                                await stream.CopyToAsync(stream2);
                                find1 = true;
                            }

                            if (find && find1)
                                break;
                        }

                        if (!find)
                            break;

                        var obj = JsonConvert.DeserializeObject<HMCLObj>
                            (Encoding.UTF8.GetString(stream1.ToArray()));

                        if (obj == null)
                            break;

                        game = obj.ToColorMC();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            game.Name = name;
                        }
                        if (!string.IsNullOrWhiteSpace(group))
                        {
                            game.GroupName = group;
                        }

                        game = await CreateGame(game);

                        if (game == null)
                            break;

                        string overrides = "overrides";

                        if (find1)
                        {
                            var obj1 = JsonConvert.DeserializeObject<CurseForgePackObj>
                                (Encoding.UTF8.GetString(stream2.ToArray()));
                            if (obj1 != null)
                            {
                                overrides = obj1.overrides;
                            }
                        }

                        foreach (ZipEntry e in zFile)
                        {
                            if (e.IsFile && e.Name.StartsWith(overrides))
                            {
                                string file = Path.GetFullPath(string.Concat(game.GetGamePath(),
                                    e.Name.AsSpan(overrides.Length)));
                                if (e.Name.EndsWith("icon.png"))
                                {
                                    file = game.GetIconFile();
                                }
                                using var stream = zFile.GetInputStream(e);

                                FileInfo info2 = new(file);
                                info2.Directory?.Create();
                                using FileStream stream3 = new(file, FileMode.Create,
                                    FileAccess.ReadWrite, FileShare.ReadWrite);
                                await stream.CopyToAsync(stream3);
                            }
                        }

                        ColorMCCore.PackState?.Invoke(CoreRunState.End);
                        import = true;
                        break;
                    }
                //直接解压
                case PackType.ZipPack:
                    {
                        ColorMCCore.PackState?.Invoke(CoreRunState.Read);

                        name ??= Path.GetFileName(dir);

                        ColorMCCore.PackState?.Invoke(CoreRunState.Start);
                        game = await CreateGame(new()
                        {
                            GroupName = group,
                            Name = name!
                        });

                        if (game != null)
                        {
                            await new ZipUtils().Unzip(game!.GetGamePath(), dir, stream4!);
                            ColorMCCore.PackState?.Invoke(CoreRunState.End);
                            import = true;
                        }
                        break;
                    }
            }
        }
        catch (Exception e)
        {
            ColorMCCore.OnError?.Invoke(LanguageHelper.Get("Core.Pack.Error2"), e, false);
            Logs.Error(LanguageHelper.Get("Core.Pack.Error2"), e);
        }
        finally
        {
            stream4?.Dispose();
        }
        if (!import && game != null)
        {
            await game.Remove();
        }
        ColorMCCore.PackState?.Invoke(CoreRunState.End);
        return (import, game);
    }

    /// <summary>
    /// 安装Modrinth压缩包
    /// </summary>
    /// <param name="data">整合包信息</param>
    /// <param name="name">名字</param>
    /// <param name="group">群组</param>
    /// <returns>结果</returns>
    public static async Task<(bool, GameSettingObj?)> InstallModrinth(ModrinthVersionObj data, string? name, string? group)
    {
        var file = data.files.FirstOrDefault(a => a.primary) ?? data.files[0];
        var item = new DownloadItemObj()
        {
            Url = file.url,
            Name = file.filename,
            SHA1 = file.hashes.sha1,
            Local = Path.GetFullPath(DownloadManager.DownloadDir + "/" + file.filename),
        };

        var res1 = await DownloadManager.Start(new() { item });
        if (!res1)
            return (false, null);

        var res2 = await InstallZip(item.Local, PackType.Modrinth, name, group);
        if (res2.Item1)
        {
            res2.Item2!.PID = data.project_id;
            res2.Item2.FID = data.id;
            res2.Item2.Save();
        }

        return res2;
    }

    /// <summary>
    /// 安装curseforge压缩包
    /// </summary>
    /// <param name="data">整合包信息</param>
    /// <param name="name">名字</param>
    /// <param name="group">群组</param>
    /// <returns>结果</returns>
    public static async Task<(bool, GameSettingObj?)> InstallCurseForge(CurseForgeModObj.Data data, string? name, string? group)
    {
        data.FixDownloadUrl();

        var item = new DownloadItemObj()
        {
            Url = data.downloadUrl,
            Name = data.fileName,
            Local = Path.GetFullPath(DownloadManager.DownloadDir + "/" + data.fileName),
        };

        var res1 = await DownloadManager.Start(new() { item });
        if (!res1)
            return (false, null);

        var res2 = await InstallZip(item.Local, PackType.CurseForge, name, group);
        if (res2.Item1)
        {
            res2.Item2!.PID = data.modId.ToString();
            res2.Item2.FID = data.id.ToString();
            res2.Item2.Save();
        }

        return res2;
    }

    /// <summary>
    /// 升级整合包
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="data">数据</param>
    /// <returns>结果</returns>
    public static async Task<bool> UpdateModPack(this GameSettingObj obj, CurseForgeModObj.Data data)
    {
        data.FixDownloadUrl();

        var item = new DownloadItemObj()
        {
            Url = data.downloadUrl,
            Name = data.fileName,
            Local = Path.GetFullPath(DownloadManager.DownloadDir + "/" + data.fileName),
        };

        var res = await DownloadManager.Start(new() { item });
        if (!res)
            return false;

        res = await ModPackHelper.UpdateCurseForgeModPack(obj, item.Local);
        if (res)
        {
            obj.PID = data.modId.ToString();
            obj.FID = data.id.ToString();
            obj.Save();
            obj.SaveModInfo();
        }

        return res;
    }

    /// <summary>
    /// 升级整合包
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="data"></param>
    /// <returns>升级结果</returns>
    public static async Task<bool> UpdateModPack(this GameSettingObj obj, ModrinthVersionObj data)
    {
        var file = data.files.FirstOrDefault(a => a.primary) ?? data.files[0];
        var item = new DownloadItemObj()
        {
            Url = file.url,
            Name = file.filename,
            SHA1 = file.hashes.sha1,
            Local = Path.GetFullPath(DownloadManager.DownloadDir + "/" + file.filename),
        };

        var res = await DownloadManager.Start(new() { item });
        if (!res)
            return false;

        res = await ModPackHelper.UpdateModrinthModPack(obj, item.Local);
        if (res)
        {
            obj.PID = data.project_id;
            obj.FID = data.id;
            obj.Save();
            obj.SaveModInfo();
        }

        return res;
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="local">目标地址</param>
    /// <param name="unselect">未选择的文件</param>
    /// <returns></returns>
    public static async Task<(bool, Exception?)> CopyFile(this GameSettingObj obj, string local, List<string> unselect)
    {
        try
        {
            local = Path.GetFullPath(local);
            var list = PathHelper.GetAllFile(local);
            list.RemoveAll(item => unselect.Contains(item.FullName));
            int basel = local.Length;
            var local1 = obj.GetGamePath();
            await Task.Run(() =>
            {
                foreach (var item in list)
                {
                    var path = item.FullName[basel..];
                    var info = new FileInfo(Path.GetFullPath(local1 + "/" + path));
                    info.Directory?.Create();
                    PathHelper.CopyFile(item.FullName, info.FullName);
                }
            });
            return (true, null);
        }
        catch (Exception e)
        {
            string temp = LanguageHelper.Get("Core.Game.Error13");
            Logs.Error(temp, e);
            return (false, e);
        }
    }
}