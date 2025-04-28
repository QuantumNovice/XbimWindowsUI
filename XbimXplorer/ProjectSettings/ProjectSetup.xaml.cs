using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;



namespace XbimXplorer.ProjectSetup
{
	public class CertificationItem
	{
		public string Title { get; set; }
		public int Score { get; set; }
		public bool Selected { get; set; }
	}

	public class CertificationCategory
	{
		public string Type { get; set; }
		public ObservableCollection<CertificationItem> Items { get; set; } = new ObservableCollection<CertificationItem>();
	}

	public partial class ProjectSetup : Window

	{
		public ProjectSetup()
		{
			InitializeComponent();

			ObservableCollection<CertificationCategory> categories = new ObservableCollection<CertificationCategory>();

			// 1. Contextual Model Verification
			var contextual = new CertificationCategory { Type = "Contextual Model Verification" };
			contextual.Items.Add(new CertificationItem { Title = "Check Wi-Fi AP", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Check Cable Data", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Check AP Simulation Data", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Check Electrical Boxes", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Disconnected Cable Check", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Check Power Storage", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Elevator Check", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Transformer Check", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Weather Proof Receptacle Check", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "PV Inverter Check", Score = 1 });
			contextual.Items.Add(new CertificationItem { Title = "Disconnect Switch Check", Score = 1 });

			categories.Add(contextual);

			// 2. ICT - Grade A
			var gradeA = new CertificationCategory { Type = "ICT - Grade A (특등급)" };
			gradeA.Items.Add(new CertificationItem { Title = "Check Clearance Gap Cables" });
			gradeA.Items.Add(new CertificationItem { Title = "Check Clearance Gap Piping" });
			gradeA.Items.Add(new CertificationItem { Title = "Check Outlet Location in Room" });
			gradeA.Items.Add(new CertificationItem { Title = "Check Digital Broadcast Devices" });
			gradeA.Items.Add(new CertificationItem { Title = "Check Cable Performance" });
			gradeA.Items.Add(new CertificationItem { Title = "Check Number of Connections" });
			categories.Add(gradeA);

			// 3. ICT - Grade AA
			var gradeAA = new CertificationCategory { Type = "ICT - Grade AA (1등급)" };
			gradeAA.Items.Add(new CertificationItem { Title = "Check Communication Room" });
			categories.Add(gradeAA);

			// 4. ICT - Grade AAA
			var gradeAAA = new CertificationCategory { Type = "ICT - Grade AAA (2등급)" };
			categories.Add(gradeAAA);

			// Bind to TreeView
			CertificationTree.ItemsSource = categories;
			


		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// start animation
			Storyboard sb = (Storyboard)this.Resources["StartAnimation"];
			sb.Begin();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{

		}
	}
}
