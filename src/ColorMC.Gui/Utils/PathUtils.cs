using Avalonia.Platform.Storage;
using ColorMC.Core.Helpers;
using ColorMC.Core.Objs;
using ColorMC.Core.Utils;
using System.Diagnostics;
using System;
using System.IO;
using System.Reflection;

namespace ColorMC.Gui.Utils;

/// <summary>
/// 路径处理
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// 目录转字符串
    /// </summary>
    /// <param name="file">路径</param>
    /// <returns>路径字符串</returns>
    public static string? GetPath(this IStorageFolder file)
    {
        if (SystemInfo.Os == OsType.Android)
        {
            return file.Path.AbsoluteUri;
        }
        return file.Path.LocalPath;
    }
    /// <summary>
    /// 文件转字符串
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>路径字符串</returns>
    public static string? GetPath(this IStorageFile file)
    {
        if (SystemInfo.Os == OsType.Android)
        {
            return file.Path.AbsoluteUri;
        }
        return file.Path.LocalPath;
    }

    /// <summary>
    /// 文件转字符串
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>路径字符串</returns>
    public static string? GetPath(this IStorageItem file)
    {
        if (SystemInfo.Os == OsType.Android)
        {
            return file.Path.AbsoluteUri;
        }
        return file.Path.LocalPath;
    }

    /// <summary>
    /// 从资源文件获取文件二进制
    /// </summary>
    /// <param name="name">文件名</param>
    /// <returns>数据</returns>
    public static byte[] GetFile(string name)
    {
        var assm = Assembly.GetExecutingAssembly();
        var item = assm.GetManifestResourceStream(name);
        using MemoryStream stream = new();
        item!.CopyTo(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 提升权限
    /// </summary>
    /// <param name="path">文件</param>
    public static void Chmod(string path)
    {
        try
        {
            using var p = new Process();
            p.StartInfo.FileName = "sh";
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            p.StandardInput.WriteLine("chmod a+x " + path);

            p.StandardInput.WriteLine("exit");
            p.WaitForExit();
        }
        catch (Exception e)
        {
            Logs.Error("chmod error", e);
        }
    }
}
