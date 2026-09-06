// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Windows.Media;

namespace FluentFlyoutWPF.Models;

public partial class AudioSessionModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly AudioSessionControl _sessionControl;
    private bool _syncing;

    [ObservableProperty]
    public partial string DisplayName { get; set; }

    [ObservableProperty]
    public partial int ProcessId { get; set; }

    [ObservableProperty]
    public partial AudioSessionState State { get; set; }

    [ObservableProperty]
    public partial float Volume { get; set; }

    [ObservableProperty]
    public partial bool IsMuted { get; set; }

    public ImageSource? Icon { get; }

    public bool HasIcon => Icon != null;

    public bool IsActive => State == AudioSessionState.AudioSessionStateActive;

    public event EventHandler? VolumeChanged;

    public AudioSessionModel(AudioSessionControl sessionControl, string displayName, int processId, AudioSessionState sessionState, ImageSource? icon)
    {
        _sessionControl = sessionControl;
        DisplayName = displayName;
        ProcessId = processId;
        State = sessionState;
        Icon = icon;
        Volume = _sessionControl.SimpleAudioVolume.Volume;
        IsMuted = _sessionControl.SimpleAudioVolume.Mute;
    }

    partial void OnVolumeChanged(float value)
    {
        if (_syncing) return;

        try
        {
            _sessionControl.SimpleAudioVolume.Volume = Math.Clamp(value, 0f, 1f);
        }
        catch (Exception ex)
        {
            // The owning process (and with it the audio session) can exit between
            // enumeration and this write; the COMException escaping the
            // ObservableProperty setter inside WPF binding killed the process.
            Logger.Debug(ex, "Failed to set volume for session (process {0})", ProcessId);
            return; // keep the stale in-memory value
        }

        // Mute is an independent toggle: forcing IsMuted to follow Volume==0
        // made a dragged-to-zero slider flip the session's mute flag and flip
        // it back on the way up, desyncing the mixer row from the real
        // session state (SetVolume syncs IsMuted from the session instead).
        VolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIsMutedChanged(bool value)
    {
        if (_syncing) return;

        try
        {
            _sessionControl.SimpleAudioVolume.Mute = value;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to set mute for session (process {0})", ProcessId);
        }
    }

    public void AdjustVolume(float delta)
    {
        Volume = Math.Clamp(Volume + delta, 0f, 1f);
    }

    [RelayCommand]
    private void ToggleMute() => IsMuted = !IsMuted;

    /// <summary>
    /// refreshes Volume and IsMuted from the audio session without pushing changes back
    /// </summary>
    public void SyncFromDevice()
    {
        _syncing = true;
        try
        {
            var vol = _sessionControl.SimpleAudioVolume.Volume;
            var mute = _sessionControl.SimpleAudioVolume.Mute;

            if (MathF.Abs(Volume - vol) > 0.001f)
                Volume = vol;

            if (IsMuted != mute)
                IsMuted = mute;
        }
        finally
        {
            _syncing = false;
        }
    }
}