using ColorMC.Core.Helpers;
using ColorMC.Core.LaunchPath;
using ColorMC.Core.Nbt;
using ColorMC.Core.Objs;
using ColorMC.Core.Objs.Minecraft;
using ColorMC.Core.Utils;
using System.Collections.Concurrent;

namespace ColorMC.Core.Game;

/// <summary>
/// 结构文件操作
/// </summary>
public static class Schematic
{
    public const string Name1 = ".litematic";
    public const string Name2 = ".schematic";

    /// <summary>
    /// 读取结构文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <returns>列表</returns>
    public static async Task<ConcurrentBag<SchematicObj>> GetSchematics(this GameSettingObj obj)
    {
        var list = new ConcurrentBag<SchematicObj>();
        var path = obj.GetSchematicsPath();

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return list;
        }

        var items = Directory.GetFiles(path);
        await Parallel.ForEachAsync(items, async (item, cancel) =>
        {
            var info = new FileInfo(item);
            if (info.Extension.ToLower() == Name1)
            {
                list.Add(await ReadAsLitematic(item));
            }
            else if (info.Extension.ToLower() == Name2)
            {
                list.Add(await ReadAsSchematic(item));
            }
        });

        return list;
    }

    /// <summary>
    /// 删除结构文件
    /// </summary>
    /// <param name="obj">结构文件</param>
    public static void Delete(this SchematicObj obj)
    {
        PathHelper.Delete(obj.Local);
    }

    /// <summary>
    /// 添加结构文件
    /// </summary>
    /// <param name="obj">游戏实例</param>
    /// <param name="file">文件列表</param>
    /// <returns>是否添加成功</returns>
    public static bool AddSchematic(this GameSettingObj obj, List<string> file)
    {
        var path = obj.GetSchematicsPath();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        var ok = true;

        Parallel.ForEach(file, (item) =>
        {
            var name = Path.GetFileName(item);
            var path1 = Path.GetFullPath(path + "/" + name);
            if (File.Exists(path1))
            {
                return;
            }

            try
            {
                PathHelper.CopyFile(item, path1);
            }
            catch (Exception e)
            {
                Logs.Error(LanguageHelper.Get("Core.Game.Error3"), e);
                ok = false;
            }
        });

        if (!ok)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 读取结构文件
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>数据</returns>
    private static async Task<SchematicObj> ReadAsSchematic(string file)
    {
        try
        {
            if (await NbtBase.Read(file) is not NbtCompound tag)
            {
                return new()
                {
                    Local = file,
                    Broken = true
                };
            }

            return new()
            {
                Name = Path.GetFileName(file),
                Height = (tag["Height"] as NbtShort)!.Value,
                Length = (tag["Length"] as NbtShort)!.Value,
                Width = (tag["Width"] as NbtShort)!.Value,
                Broken = false,
                Local = file
            };
        }
        catch (Exception e)
        {
            Logs.Error(LanguageHelper.Get("Core.Game.Error12"), e);
            return new()
            {
                Local = file,
                Broken = true
            };
        }
    }

    /// <summary>
    /// 读取结构文件
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>数据</returns>
    private static async Task<SchematicObj> ReadAsLitematic(string file)
    {
        try
        {
            if (await NbtBase.Read(file) is not NbtCompound tag)
            {
                return new()
                {
                    Local = file,
                    Broken = true
                };
            }

            var com1 = (tag["Metadata"] as NbtCompound)!;

            var item = new SchematicObj()
            {
                Name = (com1["Name"] as NbtString)!.Value,
                Author = (com1["Author"] as NbtString)!.Value,
                Description = (com1["Description"] as NbtString)!.Value,
                Broken = false,
                Local = file
            };

            var pos = (com1["EnclosingSize"] as NbtCompound)!;
            if (pos != null)
            {
                item.Height = (pos["y"] as NbtInt)!.Value;
                item.Length = (pos["x"] as NbtInt)!.Value;
                item.Width = (pos["z"] as NbtInt)!.Value;
            }

            return item;
        }
        catch (Exception e)
        {
            Logs.Error(LanguageHelper.Get("Core.Game.Error12"), e);
            return new()
            {
                Local = file,
                Broken = true
            };
        }
    }
}
