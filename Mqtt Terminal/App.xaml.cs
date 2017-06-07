using System.Windows;

namespace Mqtt_Terminal
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            new MainWindow("Dummy", new FirstView()).Show();
        }
    }
}
