using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SafetySharp.CaseStudies.Visualizations.Resources.Circuits
{
	/// <summary>
	/// Interaktionslogik für LoadCircuitContact.xaml
	/// </summary>
	public partial class LoadCircuitContact : UserControl
	{
		private readonly Storyboard _storyboardClose;
		private readonly Storyboard _storyboardOpen;

		private enum LastVisualState
		{
			Unset,
			Closed,
			Open
		}

		private LastVisualState _lastVisualState;

		public Func<bool> IsClosed { get; set; }

		public LoadCircuitContact()
		{
			_lastVisualState=LastVisualState.Unset;
			InitializeComponent();
			_storyboardClose = (Storyboard)Resources["CloseCircuitStoryBoard"];
			_storyboardOpen = (Storyboard)Resources["OpenCircuitStoryBoard"];
		}

		public void Update()
		{
			var shouldBeClosed = IsClosed();

			if (shouldBeClosed && _lastVisualState != LastVisualState.Closed)
			{
				_lastVisualState = LastVisualState.Closed;
				_storyboardOpen.Stop();
				_storyboardClose.Begin();
			}
			else if (shouldBeClosed == false && _lastVisualState != LastVisualState.Open)
			{
				_lastVisualState = LastVisualState.Open;
				_storyboardClose.Stop();
				_storyboardOpen.Begin();
			}
		}
	}
}
