using System.Windows;

namespace DemoWpfApp;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void GreetButton_Click(object sender, RoutedEventArgs e)
    {
        var name = string.IsNullOrWhiteSpace(InputBox.Text) ? "World" : InputBox.Text.Trim();
        var greeting = $"Hello, {name}!";
        if (ShoutCheck.IsChecked == true) greeting = greeting.ToUpperInvariant();
        OutputLabel.Text = greeting;
    }
}
