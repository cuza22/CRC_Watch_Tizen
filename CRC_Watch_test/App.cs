using System;
using System.Collections.Generic;
using System.Timers;
using Tizen.Security;
using Xamarin.Forms;
namespace CRC_Watch_test
{
	public class App : Application
	{
		//public DataCollectingBackground data_collecting = null;
		private const string LOCATION_PRIVILEGE = "http://tizen.org/privilege/location";
		private const string HEALTHINFO_PRIVILEGE = "http://tizen.org/privilege/healthinfo";
		private const string ACCESS_PRIVILEGE = "http://tizen.org/privilege/mediastorage";


		public App()
		{
			var modeList = new List<String>
				{
					"Still", "Walking", "Manual Wheelchair", "Electric Wheelchair", "Bus", "Subway", "Car",
				};
			var menu = new ListView()
			{
				ItemsSource = modeList,
			};
			menu.ItemTapped += Menu_ItemTapped;
			// The root page of your application
			MainPage = new ContentPage
			{
				BackgroundColor = Color.White,
				Content = new StackLayout
				{
					VerticalOptions = LayoutOptions.Center,
					Children = { menu, }

				},
			};
			Console.WriteLine("Init Page");
		}

		private void Menu_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			string mode = e.Item.ToString();
			MainPage.Navigation.PushModalAsync(new Data_Collecting_Page(mode));
		}

		private void PrivilegeCheck()
		{
			// Location privacy
			try
			{
				CheckResult result = PrivacyPrivilegeManager.CheckPermission(LOCATION_PRIVILEGE);

				switch (result)
				{
					case CheckResult.Allow:
						break;
					case CheckResult.Deny:
						break;
					case CheckResult.Ask:
						PrivacyPrivilegeManager.RequestPermission(LOCATION_PRIVILEGE);
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			// Healthinfo privacy
			try
			{
				CheckResult result = PrivacyPrivilegeManager.CheckPermission(HEALTHINFO_PRIVILEGE);
				switch (result)
				{
					case CheckResult.Allow:
						break;
					case CheckResult.Deny:
						break;
					case CheckResult.Ask:
						PrivacyPrivilegeManager.RequestPermission(HEALTHINFO_PRIVILEGE);
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			// Storage(internal) privacy
			try
			{
				CheckResult result = PrivacyPrivilegeManager.CheckPermission(ACCESS_PRIVILEGE);
				switch (result)
				{
					case CheckResult.Allow:
						break;
					case CheckResult.Deny:
						break;
					case CheckResult.Ask:
						PrivacyPrivilegeManager.RequestPermission(ACCESS_PRIVILEGE);
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		protected override void OnStart()
		{
			// Handle when your app starts
			PrivilegeCheck();
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
			// keep collecting data
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
			// nothing
		}
		public class Data_Collecting_Page : ContentPage
		{
			public Data_Collecting_Page(string mode)
			{
				int seconds_remaining = Global.TOTAL_SECONDS;

				var label = new Label()
				{
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					Text = "Data Collecting",
				};
				var timerLabel = new Label()
				{
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					Text = seconds_remaining + "sec remaining..."
				};
				var layout = new StackLayout
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Children = {
							label,
							timerLabel
						},
				};
				Content = layout;

				Console.WriteLine("Data Collecting Page");
				DataCollectingBackground data_collecting = new DataCollectingBackground(mode);
				data_collecting.startDataCollecting();

				Timer timer = new Timer();
				timer.Interval = 1000;
				timer.Elapsed += new ElapsedEventHandler(handler);
				void handler(object sender, ElapsedEventArgs e)
				{
					seconds_remaining -= 1;
					// change ui
					timerLabel.Text = seconds_remaining.ToString() + "sec remaining...";
					//layout.Children.Remove(timerLabel);
					//layout.Children.Add(timerLabel);
					//Content = layout;


					Console.WriteLine(seconds_remaining + "sec remaining\n");

					if (seconds_remaining < 0)
					{
						Navigation.PushModalAsync(new Data_Collecting_End_Page());
						timer.Dispose();
						data_collecting.endDataCollecting();
					}
				};
				timer.Start();

			}

		}
		public class Data_Collecting_End_Page : ContentPage
		{
			public Data_Collecting_End_Page()
			{
				var label = new Label()
				{
					Text = "End Page",
				};
				Content = new StackLayout
				{
					Children = { label, },
				};
			}
			//Console.WriteLine("End page");
		}
	}
}
