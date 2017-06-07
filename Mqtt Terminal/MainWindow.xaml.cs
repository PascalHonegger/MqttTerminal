using System.Windows;
using System.Windows.Controls;

namespace Mqtt_Terminal
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string title, UserControl content)
        {
            InitializeComponent();

            Title = title;
            TitleText.Text = title;

            MainContent.Content = content;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
