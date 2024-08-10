﻿using System;
using System.Drawing;
using System.Windows;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly static string _defaultIconPath = "Images/clipboard.png";

    private readonly Image _defaultImage = new Bitmap(_defaultIconPath);

    public MainWindow()
    {
        InitializeComponent();
        PreviewTextTabItem.Content = new PreviewPanel(null!, GetRandomClipboardData(DataType.Text));
        PreviewImageTabItem.Content = new PreviewPanel(null!, GetRandomClipboardData(DataType.Image));
        PreviewFilesTabItem.Content = new PreviewPanel(null!, GetRandomClipboardData(DataType.Files));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.ViewModel.OnCultureInfoChanged(null!);
    }

    private ClipboardData GetRandomClipboardData(DataType type)
    {
        var rand = new Random();
        var data = new ClipboardData()
        {
            HashId = StringUtils.GetGuid(),
            Text = StringUtils.RandomString(10),
            DataType = type,
            Data = StringUtils.RandomString(10),
            SenderApp = StringUtils.RandomString(5) + ".exe",
            Title = StringUtils.RandomString(10),
            PreviewImagePath = _defaultIconPath,
            Score = rand.Next(1000),
            InitScore = rand.Next(1000),
            Pinned = false,
            CreateTime = DateTime.Now,
        };
        if (data.DataType == DataType.Image)
        {
            data.Data = _defaultImage;
        }
        else if (data.DataType == DataType.Files)
        {
            data.Data = new string[] { StringUtils.RandomString(10), StringUtils.RandomString(10) };
        }
        return data;
    }
}