﻿using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using ColorMC.Core;
using System;
using System.ComponentModel;
using System.Threading;

namespace ColorMC.Gui.Utils.LaunchSetting;

public class Colors : INotifyPropertyChanged
{
    public static readonly IBrush AppBackColor = Brush.Parse("#FFFFFFFF");
    public static readonly IBrush AppBackColor1 = Brush.Parse("#11FFFFFF");

    public static IBrush MainColor = Brush.Parse("#FF5ABED6");
    public static IBrush BackColor = Brush.Parse("#FFF4F4F5");
    public static IBrush Back1Color = Brush.Parse("#88FFFFFF");
    public static IBrush ButtonFont = Brush.Parse("#FFFFFFFF");
    public static IBrush FontColor = Brush.Parse("#FF000000");
    public static IBrush MotdColor = Brush.Parse("#FFFFFFFF");
    public static IBrush MotdBackColor = Brush.Parse("#FF000000");

    public static Colors Instance { get; set; } = new Colors();

    private const string IndexerName = "Item";
    private const string IndexerArrayName = "Item[]";

    public static void Load()
    {
        try
        {
            if (GuiConfigUtils.Config.RGB == true)
            {
                Instance.EnableRGB();
            }
            else
            {
                Instance.DisableRGB();
                MainColor = Brush.Parse(GuiConfigUtils.Config.ColorMain);
                BackColor = Brush.Parse(GuiConfigUtils.Config.ColorBack);
                Back1Color = Brush.Parse(GuiConfigUtils.Config.ColorTranBack);
                ButtonFont = Brush.Parse(GuiConfigUtils.Config.ColorFont1);
                FontColor = Brush.Parse(GuiConfigUtils.Config.ColorFont2);

                MotdColor = Brush.Parse(GuiConfigUtils.Config.ServerCustom.MotdColor);
                MotdBackColor = Brush.Parse(GuiConfigUtils.Config.ServerCustom.MotdBackColor);

                Instance.Reload();
            }
        }
        catch (Exception e)
        {
            Logs.Error("颜色数据读取失败", e);
        }
    }

    private Thread timer;
    private bool rbg;
    private bool run;
    private double rbg_s = 1;
    private double rbg_v = 1;

    public Colors()
    {
        timer = new(Tick)
        {
            Name = "ColorMC-RGB"
        };
        run = true;
        timer.Start();
    }

    public void Stop()
    {
        run = false;
    }

    public void EnableRGB()
    {
        rbg = true;

        rbg_s = (double)GuiConfigUtils.Config.RGBS / 100;
        rbg_v = (double)GuiConfigUtils.Config.RGBV / 100;
    }

    public void DisableRGB()
    {
        rbg = false;
    }

    private int now;
    private IBrush Color = MainColor;
    private IBrush Color1 = FontColor;

    private void Tick(object? obj)
    {
        while (run)
        {
            if (rbg)
            {
                now += 1;
                now %= 360;
                var temp = HsvColor.ToRgb(now, rbg_s, rbg_v);
                Color = new ImmutableSolidColorBrush(temp);
                if (rbg_v >= 0.8)
                {
                    if (now == 190)
                    {
                        Color1 = Brush.Parse("#FFFFFFFF");
                    }
                    else if (now == 10)
                    {
                        Color1 = Brush.Parse("#FF000000");
                    }
                }
                else
                {
                    Color1 = Brush.Parse("#FFFFFFFF");
                }

                Dispatcher.UIThread.InvokeAsync(Reload).Wait();
            }
            Thread.Sleep(20);
        }
    }

    public IBrush this[string key]
    {
        get
        {
            if (key == "Main")
                return rbg ? Color : MainColor;
            else if (key == "Back")
                return BackColor;
            else if (key == "TranBack")
                return Back1Color;
            else if (key == "Font")
                return FontColor;
            else if (key == "ButtonFont")
                return rbg ? Color1 : ButtonFont;
            else if (key == "Motd")
                return MotdColor;
            else if (key == "MotdBack")
                return MotdBackColor;

            return Brushes.White;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Reload()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerArrayName));
    }
}
