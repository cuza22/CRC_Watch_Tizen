﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Tizen.Location;
using Tizen.Security;
using Tizen.Sensor;
using Tizen.System;
using Xamarin.Forms;
namespace CRC_Watch_test
{
	public static class Global
	{
		public const double FREQUENCY = 60;
		public const double GPS_INTERVAL = 5;
		public const int TOTAL_SECONDS = 660;
	}
	public class App : Application
	{
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
				CheckResult result = PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/location");

				switch (result)
				{
					case CheckResult.Allow:
						break;
					case CheckResult.Deny:
						break;
					case CheckResult.Ask:
						PrivacyPrivilegeManager.RequestPermission("http://tizen.org/privilege/location");
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
				CheckResult result = PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/healthinfo");
				switch (result)
				{
					case CheckResult.Allow:
						break;
					case CheckResult.Deny:
						break;
					case CheckResult.Ask:
						PrivacyPrivilegeManager.RequestPermission("http://tizen.org/privilege/healthinfo");
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
				CheckResult result = PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/mediastorage");
				switch (result)
				{
					case CheckResult.Allow:
						break;
					case CheckResult.Deny:
						break;
					case CheckResult.Ask:
						PrivacyPrivilegeManager.RequestPermission("http://tizen.org/privilege/mediastorage");
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
			// save data and kill app
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
			// nothing
		}
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
				layout.Children.Remove(timerLabel);
				layout.Children.Add(timerLabel);
				Content = layout;


				Console.WriteLine(seconds_remaining + "sec remaining\n");

				if (seconds_remaining <= 0)
				{
					timer.Dispose();
					data_collecting.endDataCollecting();
					Navigation.PushModalAsync(new Data_Collecting_End_Page());
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
	public class DataCollectingBackground
	{
		// delegates
		bool isCollectingData = false;

		Stopwatch stopwatch = new Stopwatch();// for checking elapsed time
		Timer SensorTimer = new Timer(); // for calling events
		Timer GPSTimer = new Timer();

		public float accX, accY, accZ;
		public float gyroX, gyroY, gyroZ;
		public float magX, magY, magZ;
		public float graX, graY, graZ;
		public float rotVecX, rotVecY, rotVecZ, rotVecW;
		public float LAccX, LAccY, LAccZ;
		public float yaw, pitch, roll;
		public float light;
		public float pressure;
		public int heartrate;
		public int heartrate_batch;

		public float latitude, longitude, altitude, speed, direction, accuracy;

		private static Accelerometer accelerometer = null;
		private static Gyroscope gyroscope = null;
		private static Magnetometer magnetometer = null;
		private static GravitySensor gravitySensor = null;
		private static GyroscopeRotationVectorSensor gyroscopeRotationVectorSensor = null;
		private static LinearAccelerationSensor linearAccelerationSensor = null;
		private static OrientationSensor orientation = null;
		private static LightSensor lightSensor = null;
		private static PressureSensor pressureSensor = null;
		private static HeartRateMonitor heartbeatMonitor = null;

		private static Locator locator = null;
		private static GpsSatellite gpsSatellite = null;
		private static Location location = null;

		public string sensor_data = "";
		public string gps_data = "";

		string mode = "";

		static public string sensor_header = "Time, Year, Month, Day, Hour, Min, Sec, " +
							"AccX, AccY, AccZ, GyroX, GyroY, GyroZ, MagX, MagY, MagZ, " +
							"GraX, GraY, GraZ, RotVec_0, RotVec_1, RotVec_2, RotVec_3, " +
							"LAccX, LAccY, LAccZ, Yaw, Pitch, Roll, Light, Pressure, " +
							"Heartrate\n";
		static private string gps_header = "Time, Year, Month, Day, Hour, Min, Sec, Latitude, Longitude, Altitude, Speed, Bearing, Accuracy\n";


		public DataCollectingBackground(string mode)
		{
			this.mode = mode;
		}
		public void startDataCollecting()
		{
			Console.WriteLine("Start data collecting");
			InitSensors();
			InitGPS();
			StartTimers();
		}
		public void endDataCollecting()
		{
			Console.WriteLine("End data collecting");
			DeinitSensors();
			DeinitGPS();

			SaveAsCSV(mode, "SensorData", sensor_data);
			SaveAsCSV(mode, "GPSData", gps_data);
		}

		void InitSensors()
		{
			if (Accelerometer.IsSupported)
			{
				Console.WriteLine("[sensor] accelerometer supported\n");
				try { accelerometer = new Accelerometer(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				accelerometer.DataUpdated += Accelerometer_DataUpdated;
				accelerometer.Start();
			}
			if (Gyroscope.IsSupported)
			{
				Console.WriteLine("[sensor] gyroscope supported\n");

				try { gyroscope = new Gyroscope(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				gyroscope.DataUpdated += Gyroscope_DataUpdated;
				gyroscope.Start();
			}
			if (Magnetometer.IsSupported)
			{
				Console.WriteLine("[sensor] magnetometer supported\n");
				try { magnetometer = new Magnetometer(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				magnetometer.DataUpdated += Magnetometer_DataUpdated;
				magnetometer.Start();
			}
			if (GravitySensor.IsSupported)
			{
				Console.WriteLine("[sensor] gravity sensor supported\n");
				try { gravitySensor = new GravitySensor(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				gravitySensor.DataUpdated += GravitySensor_DataUpdated;
				gravitySensor.Start();
			}
			if (GyroscopeRotationVectorSensor.IsSupported)
			{
				Console.WriteLine("[sensor] rotation vector supported\n");
				try { gyroscopeRotationVectorSensor = new GyroscopeRotationVectorSensor(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				gyroscopeRotationVectorSensor.DataUpdated += GyroscopeRotationVectorSensor_DataUpdated;
				gyroscopeRotationVectorSensor.Start();
			}
			if (LinearAccelerationSensor.IsSupported)
			{
				Console.WriteLine("[sensor] linear acceleration supported\n");
				try {  linearAccelerationSensor = new LinearAccelerationSensor(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				linearAccelerationSensor.DataUpdated += LinearAccelerationSensor_DataUpdated;
				linearAccelerationSensor.Start();
			}
			if (OrientationSensor.IsSupported)
			{
				Console.WriteLine("[sensor] orientation supported\n");
				try { orientation = new OrientationSensor(); } catch (Exception ex) { Console.WriteLine(ex.Message);  }
				orientation.DataUpdated += Orientation_DataUpdated;
				orientation.Start();
			}
			if (LightSensor.IsSupported)
			{
				Console.WriteLine("[sensor] light sensor supported\n");
				try { lightSensor = new LightSensor(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				lightSensor.DataUpdated += LightSensor_DataUpdated;
				lightSensor.Start();
			}
			if (PressureSensor.IsSupported)
			{
				Console.WriteLine("[sensor] pressure sensor supported\n");
				try { pressureSensor = new PressureSensor();} catch (Exception ex) { Console.WriteLine(ex.Message); }
				pressureSensor.DataUpdated += PressureSensor_DataUpdated;
				pressureSensor.Start();
			}
			if (HeartRateMonitor.IsSupported)
			{
				Console.WriteLine("[sensor] heartrate monitor supported\n");
				try { heartbeatMonitor = new HeartRateMonitor(); } catch (Exception ex) { Console.WriteLine(ex.Message); }
				heartbeatMonitor.DataUpdated += HeartbeatMonitor_DataUpdated;
				heartbeatMonitor.Start();
			}

			sensor_data += sensor_header;
		}

		private void Orientation_DataUpdated(object sender, OrientationSensorDataUpdatedEventArgs e)
		{
			yaw = e.Azimuth;
			pitch = e.Pitch;
			roll = e.Roll;
		}

		private void PressureSensor_DataUpdated(object sender, PressureSensorDataUpdatedEventArgs e)
		{
			pressure = e.Pressure;
		}

		private void LightSensor_DataUpdated(object sender, LightSensorDataUpdatedEventArgs e)
		{
			light = e.Level;
		}

		private void LinearAccelerationSensor_DataUpdated(object sender, LinearAccelerationSensorDataUpdatedEventArgs e)
		{
			LAccX = e.X;
			LAccY = e.Y;
			LAccZ = e.Z;
		}

		private void HeartbeatMonitor_DataUpdated(object sender, HeartRateMonitorDataUpdatedEventArgs e)
		{
			heartrate = e.HeartRate;
		}

		private void GyroscopeRotationVectorSensor_DataUpdated(object sender, GyroscopeRotationVectorSensorDataUpdatedEventArgs e)
		{
			rotVecX = e.X;
			rotVecY = e.Y;
			rotVecZ = e.Z;
			rotVecW = e.W;
		}

		private void GravitySensor_DataUpdated(object sender, GravitySensorDataUpdatedEventArgs e)
		{
			graX = e.X;
			graY = e.Y;
			graZ = e.Z;
		}

		private void Accelerometer_DataUpdated(object sender, AccelerometerDataUpdatedEventArgs e)
		{
			//Console.WriteLine("accX: {0}, accY: {1}, accZ: {2}", e.X, e.Y, e.Z);
			accX = e.X;
			accY = e.Y;
			accZ = e.Z;
		}
		private void Gyroscope_DataUpdated(object sender, GyroscopeDataUpdatedEventArgs e)
		{
			gyroX = e.X;
			gyroY = e.Y;
			gyroZ = e.Z;
		}
		private void Magnetometer_DataUpdated(object sender, MagnetometerDataUpdatedEventArgs e)
		{
			magX = e.X;
			magY = e.Y;
			magZ = e.Z;
		}
		void DeinitSensors()
		{
			Console.WriteLine("deinit sensors\n");
			if (accelerometer != null) { accelerometer.Dispose(); }
			if (gyroscope != null) { gyroscope.Dispose(); }
			if (magnetometer != null) { magnetometer.Dispose();}
			if (gravitySensor != null) { gravitySensor.Dispose();}
			if (gyroscopeRotationVectorSensor != null) { gyroscopeRotationVectorSensor.Dispose(); }
			if (linearAccelerationSensor != null) { linearAccelerationSensor.Dispose(); }
			if (lightSensor != null) { lightSensor.Dispose(); }
			if (pressureSensor != null) { pressureSensor.Dispose(); }
			if (heartbeatMonitor != null) { heartbeatMonitor.Dispose(); }
			SensorTimer.Enabled = false;
			Console.WriteLine("sensor timer disabled\n");
		}
		void InitGPS()
		{
			if (locator == null)
			{
				try
				{
					locator = new Locator(LocationType.Hybrid); // LocationType.Gps (more accurate)
					gpsSatellite = new GpsSatellite(locator);

					if (locator != null)
					{
						Console.WriteLine("[GPS]locator is not null\n");
						locator.Start();
						locator.LocationChanged += LocationChangedHandler;
					}
					//location = locator.GetLocation(); // TODO : LocationChangedHandler로 대체 가능?

				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}

			gps_data += gps_header;
		}
		void DeinitGPS()
		{
			Console.WriteLine("deinit GPS\n");
			locator.Dispose();
			locator = null;
			GPSTimer.Enabled = false;
		}
		void LocationChangedHandler(Object sender, LocationChangedEventArgs e)
		{
			location = e.Location;
			latitude = (float)location.Latitude;
			longitude = (float)location.Longitude;
			altitude = (float)location.Altitude;
			speed = (float)location.Speed;
			direction = (float)location.Direction;
			accuracy = (float)location.Accuracy;
		}
		// timer control
		void StartTimers()
		{
			SensorTimer.Interval = 1000 / Global.FREQUENCY;
			SensorTimer.Elapsed += UpdateSensorDataString;
			GPSTimer.Interval = Global.GPS_INTERVAL * 1000;
			GPSTimer.Elapsed += UpdateGPSDataString;

			stopwatch.Start();
			SensorTimer.Start();
			GPSTimer.Start();
		}
		// data control
		public void UpdateSensorDataString(Object source, ElapsedEventArgs e)
		{
			long elapsedTime = (long)(stopwatch.ElapsedMilliseconds / (double)1000);
			string time = elapsedTime.ToString("0.000");
			DateTime currentTime = GetCurrentTime();
			string date = currentTime.ToString("yyyy,MM,dd,HH,mm,ss,");
			sensor_data += time + "," + date
						  + accX + "," + accY + "," + accZ + ","
						  + gyroX + "," + gyroY + "," + gyroZ + ","
						  + magX + "," + magY + "," + magZ + ","
						  + graX + "," + graY + "," + graZ + ","
						  + rotVecX + "," + rotVecY + "," + rotVecZ + "," + rotVecW + ","
						  + heartrate + "\n";
		}
		public void UpdateGPSDataString(Object source, ElapsedEventArgs e)
		{
			//Console.WriteLine("sensor data : " + sensor_data);
			//Console.WriteLine("GPS data : " + gps_data);

			long elapsedTime = (long)(stopwatch.ElapsedMilliseconds / (double)1000);
			string time = elapsedTime.ToString("0.000");
			DateTime currentTime = GetCurrentTime();
			string date = currentTime.ToString("yyyy,MM,dd,HH,mm,ss,");
			gps_data += time + "," + date + latitude + "," + longitude + ","+ altitude + "," + speed + "," + direction + "," + accuracy + "\n";
		}
		private string GetDirectory()
		{
			var storages = StorageManager.Storages;
			//Console.WriteLine("storage: " + storages);
			var internalStorage = storages.Where(s => s.StorageType == StorageArea.Internal).FirstOrDefault();
			var externalStorage = storages.Where(s => s.StorageType == StorageArea.External).FirstOrDefault();
			//Console.WriteLine("internal: " + internalStorage);
			//Console.WriteLine("external: " + externalStorage);
			var rootDir = internalStorage.RootDirectory;
			Console.WriteLine(rootDir);
			return rootDir;
		}
		private DateTime GetCurrentTime()
		{
			DateTime date = DateTime.Now;
			return date;
		}
		public void SaveAsCSV(string mode, string sensor, string data)
		{
			Console.WriteLine("Save " + sensor + " as CSV\n");
			var rootDir = GetDirectory();
			var folderDirectory = rootDir + "/TMDData/";
			if (!Directory.Exists(folderDirectory))
			{
				Directory.CreateDirectory(rootDir + "/TMDData/");
				Console.WriteLine("Directory created");
			}
			DateTime currentTime = GetCurrentTime();
			string time = currentTime.ToString("yyyy_MM_dd_HH_mm_ss_");
			string fileName = time + mode + @"_" + sensor + @".csv";
			var fileDirectory = Path.Combine(folderDirectory, fileName);
			Console.WriteLine(fileDirectory);

			//var fs = File.Create(fileDirectory);
			//Console.WriteLine("file created");
			try { File.WriteAllText(fileDirectory, data); } catch { Console.WriteLine("File not created\n"); }
			Console.WriteLine("file saved!");
		}
	}
}
