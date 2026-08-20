// MainWindow.axaml.cs
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI_Components;

public partial class MainWindow : Window
{
    public ObservableCollection<User> Users { get; } = new()
    {
        new User
        {
            Id = 1,
            Name = "Alice Johnson",
            Role = "Administrator"
        },

        new User
        {
            Id = 2,
            Name = "Bob Smith",
            Role = "Developer"
        },

        new User
        {
            Id = 3,
            Name = "Charlie Brown",
            Role = "Designer"
        },

        new User
        {
            Id = 4,
            Name = "Diana Garcia",
            Role = "HR Manager"
        },

        new User
        {
            Id = 5,
            Name = "Edward Wilson",
            Role = "Sales"
        }
    };

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        // New action
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        // Save action
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        // Refresh action
    }

    private void SaveProfile_Click(object? sender, RoutedEventArgs e)
    {
        // Save profile action
    }

    private void SavePreferences_Click(object? sender, RoutedEventArgs e)
    {
        // Save preferences action
    }
}

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}