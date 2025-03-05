﻿namespace CroomsBellScheduleCS.Views.Settings;

public sealed partial class LoginView
{
    public LoginView()
    {
        InitializeComponent();
        UsernameBox.ItemsSource = new string[] { "Suvan", "LandonLostHisAccountAgainBecauseOfSuvanAgain", };
    }
    private void ValidateFields()
    {
        if (string.IsNullOrEmpty(UsernameBox.Text))
        {
            LoginFailureText.Text = "A username is required.";
            return;
        }
        if (string.IsNullOrEmpty(PasswordBox.Password))
        {
            LoginFailureText.Text = "A password is required.";
            return;
        }
        LoginFailureText.Text = "Not implemented";
    }
    private void UsernameBox_TextChanged(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
    {
        ValidateFields();
    }

    private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ValidateFields();
    }
}