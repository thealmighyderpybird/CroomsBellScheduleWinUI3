﻿using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class BellView
{
    private bool _initializing = true;
    public BellView()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        BellScheduleReader? reader = MainWindow.ViewInstance.Reader;
        if (reader == null) return;

        string response = "";
        foreach (var item in reader.GetFilteredClasses(MainWindow.ViewInstance.LunchOffset))
        {
            if (item != null)
            {
                txtBell.Text = $"{item.StartString} - {item.EndString}: {item.Name}";
            }
        }

        for (int i = 1; i < 8; i++)
        {
            StackPanel panel = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            TextBlock time = new TextBlock()
            {
                Text = $"Period {i}: ",
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBox box = new TextBox() { Text = SettingsManager.Settings.PeriodNames[i], Margin = new Thickness(10, 0, 0, 0), Width = 300, MaxWidth = 300 };
            box.TextChanged += async delegate (object sender, TextChangedEventArgs e)
            {
                SettingsManager.Settings.PeriodNames[i] = box.Text;
                await SettingsManager.SaveSettings();
            };
            panel.Children.Add(time);
            panel.Children.Add(box);
            ContentArea.Children.Add(panel);
        }

        CmbPreferredSchedule.SelectedIndex = SettingsManager.Settings.UseLocalBellSchedule ? 1 : 0;

        txtBell.Text = response;

        _initializing = false;
    }

    private async void CmbPreferredSchedule_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;

        SettingsManager.Settings.UseLocalBellSchedule = CmbPreferredSchedule.SelectedIndex == 1;
        await MainWindow.ViewInstance.UpdateScheduleSource();
        await SettingsManager.SaveSettings();
    }
}