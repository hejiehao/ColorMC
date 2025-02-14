﻿using ColorMC.Core.Objs;
using ColorMC.Core.Utils;
using ColorMC.Gui.Objs;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace ColorMC.Gui.UI.Model.GameEdit;

public partial class GameEditModel : MenuModel
{
    public override List<MenuObj> TabItems { get; init; } =
    [
        new() { Icon = "/Resource/Icon/GameEdit/item1.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text1") },
        new() { Icon = "/Resource/Icon/GameEdit/item2.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text2") },
        new() { Icon = "/Resource/Icon/GameEdit/item3.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text4") },
        new() { Icon = "/Resource/Icon/GameEdit/item4.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text5") },
        new() { Icon = "/Resource/Icon/GameEdit/item5.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text6") },
        new() { Icon = "/Resource/Icon/GameEdit/item6.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text7") },
        new() { Icon = "/Resource/Icon/GameEdit/item7.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text10") },
        new() { Icon = "/Resource/Icon/GameEdit/item8.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text11") },
        new() { Icon = "/Resource/Icon/GameEdit/item9.svg",
            Text = App.Lang("GameEditWindow.Tabs.Text12") },
    ];

    [ObservableProperty]
    private bool _displayFilter = true;

    private readonly GameSettingObj _obj;

    public bool Phone { get; } = false;

    private readonly string _useName;

    public GameEditModel(BaseModel model, GameSettingObj obj) : base(model)
    {
        _useName = ToString() + ":" + obj.UUID;

        _obj = obj;
        if (SystemInfo.Os == OsType.Android)
        {
            Phone = true;
        }

        _titleText = string.Format(App.Lang("GameEditWindow.Tab2.Text13"), _obj.Name);
    }

    public void ShowFilter()
    {
        DisplayFilter = !DisplayFilter;
    }

    public void SetBackHeadTab()
    {
        Model.SetChoiseContent(_useName, App.Lang("Button.Refash"));
        Model.SetChoiseCall(_useName, ShowFilter);
    }

    public void RemoveBackHead()
    {
        Model.RemoveChoiseContent(_useName);
        Model.RemoveChoiseCall(_useName);
    }

    public void OpenLoad()
    {
        GameLoad();
        ConfigLoad();
    }

    protected override void Close()
    {
        _configLoad = true;
        _gameLoad = true;
        GameVersionList.Clear();
        LoaderVersionList.Clear();
        GroupList.Clear();
        JvmList.Clear();
        ModList.Clear();
        _modItems.Clear();
        foreach (var item in WorldList)
        {
            item.Close();
        }
        WorldList.Clear();
        _selectWorld = null;
        foreach (var item in ResourcePackList)
        {
            item.Close();
        }
        ResourcePackList.Clear();
        _lastResource = null;
        foreach (var item in ScreenshotList)
        {
            item.Close();
        }
        ScreenshotList.Clear();
        _lastScreenshot = null!;
        ServerList.Clear();
        ShaderpackList.Clear();
        SchematicList.Clear();
    }
}
