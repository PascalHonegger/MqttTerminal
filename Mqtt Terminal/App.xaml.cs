using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Mqtt_Terminal
{
	/// <summary>
	///     Interaktionslogik für "App.xaml"
	/// </summary>
	public partial class App
	{
		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			var culture = new CultureInfo(SettingsManager.Instance.CurrentSettings.Language);

			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
		}
	}
}