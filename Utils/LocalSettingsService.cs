﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using CroomsBellScheduleCS.Utils;

public static class LocalSettingsService
{
    private const string _defaultApplicationDataFolder = "Crooms Bell Schedule/";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";
    private static readonly string _applicationDataFolder;

    private static readonly string _localApplicationData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private static readonly string _localsettingsFile;

    static LocalSettingsService()
    {
        _applicationDataFolder = Path.Combine(_localApplicationData, _defaultApplicationDataFolder);
        _localsettingsFile = _defaultLocalSettingsFile;
    }

    public static Stream Open(bool write = false)
    {
        if (RuntimeHelper.IsMSIX)
        {
            var data = ApplicationData.Current.LocalSettings.Values["data"];
            if (data != null)
                return new MemoryStream(Encoding.UTF8.GetBytes(data.ToString() ?? "{}"));
            else return new MemoryStream();
        }
        else
        {
            string path = Path.Combine(_applicationDataFolder, _localsettingsFile);

            FileStream fs;

            if (File.Exists(path))
            {
                fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            else
            {
                Directory.CreateDirectory(_applicationDataFolder);
                File.WriteAllText(path, "{}");
                fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            }

            if (write)
            {
                fs.SetLength(0);
            }

            return fs;
        }
    }

    public static void Save(Stream s)
    {
        if (s is MemoryStream ms)
        {
            if (RuntimeHelper.IsMSIX)
            {
                ApplicationData.Current.LocalSettings.Values["data"] = Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}