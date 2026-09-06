// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;
using Windows.Storage;

namespace FluentFlyoutWPF.Classes.Utils
{
    internal class FileSystemHelper
    {
        public static string GetLogsPath()
        {
            string path;

            // the way MSIX apps choose a logs path work incredibly weirdly (i haven't figured it out), so we're searching multiple possible locations
            // first, check same path as where settings is saved
            try
            {
                path = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path,
                    "Roaming",
                    "FluentFlyout");
                if (Directory.Exists(path))
                    return path;
            }
            catch { }

            // if that doesn't work, check %appData%\FluentFlyout
            try
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FluentFlyout");
                if (Directory.Exists(path))
                    return path;
            }
            catch { }

            // if neither of those exist, check the hardcoded MSIX path before
            // returning it: handing back a nonexistent folder made the log
            // viewer run `explorer.exe <missing path>`, which just opens
            // Documents (#756). Fall back to %APPDATA%\FluentFlyout instead.
            try
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages",
                    "unchihugo.FluentFlyout_69b7b6qge1ahj",
                    "LocalCache",
                    "Roaming",
                    "FluentFlyout"
                );
                if (Directory.Exists(path))
                    return path;
            }
            catch { }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluentFlyout"
            );
        }
    }
}