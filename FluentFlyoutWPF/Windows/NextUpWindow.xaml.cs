// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyout.Classes;
using FluentFlyout.Classes.Settings;
using FluentFlyout.Classes.Utils;
using FluentFlyoutWPF.Classes;
using MicaWPF.Controls;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FluentFlyoutWPF.Windows;

/// <summary>
/// Interaction logic for NextUpWindow.xaml
/// </summary>
public partial class NextUpWindow : MicaWindow
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
    private bool _closed; // set when the window is closed before the auto-close fires

    public NextUpWindow(string title, string artist, BitmapImage thumbnail)
    {
        DataContext = SettingsManager.Current;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = -Width - 9999; // move window out of bounds to prevent flickering, maybe needs better solution
        Top = 9999;
        WindowHelper.SetNoActivate(this);
        InitializeComponent();
        WindowHelper.SetTopmost(this);
        CustomWindowChrome.CaptionHeight = 0;

        if (SettingsManager.Current.NextUpAcrylicWindowEnabled)
        {
            WindowBlurHelper.EnableBlur(this);
        }
        else
        {
            WindowBlurHelper.DisableBlur(this);
        }

        int additionalMargin = 8; // additional margin to avoid text clipping
        var upNextWidth = StringWidth.GetStringWidth(UpNextTextBlock.Text) + additionalMargin;
        var titleWidth = StringWidth.GetStringWidth(title) + additionalMargin;
        var artistWidth = StringWidth.GetStringWidth(artist) + additionalMargin;

        Width = titleWidth > artistWidth ? titleWidth + 76 + upNextWidth : artistWidth + 76 + upNextWidth;
        if (Width > 400) Width = 400; // max width to prevent window from being too wide
        SongTitle.Text = title;
        SongArtist.Text = artist;
        UpdateThumbnail(thumbnail);
        Closed += (_, _) => _closed = true;
        Show();

        mainWindow.OpenAnimation(this);

        async void wait()
        {
            try
            {
                await Task.Delay(SettingsManager.Current.NextUpDuration);

                // MainWindow closes this window early on a song change or media
                // key; the orphaned continuation used to run CloseAnimation on
                // the dead window (PointToScreen without a PresentationSource)
                // inside async void and took the process down.
                if (_closed) return;

                mainWindow.CloseAnimation(this);
                await Task.Delay(MainWindow.getDuration());
                if (_closed) return;

                Close();
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Next Up auto-close continuation failed");
            }
        }

        wait();
    }

    public void UpdateThumbnail(BitmapImage thumbnail)
    {
        SongImage.ImageSource = thumbnail;
        if (SongImage.ImageSource == null) SongImagePlaceholder.Visibility = Visibility.Visible;
        else SongImagePlaceholder.Visibility = Visibility.Collapsed;
    }
}