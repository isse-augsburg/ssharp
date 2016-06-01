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
	public partial class PowerSource : UserControl
	{
		private readonly Storyboard _storyboardEnableCurrent;
		private readonly Storyboard _storyboardDisableCurrent;

		private enum LastVisualState
		{
			Unset,
			Active,
			Inactive
		}

		public Func<bool> IsActive { get; set; }

		private LastVisualState _lastVisualState;

		public PowerSource()
		{
			_lastVisualState = LastVisualState.Unset;
			InitializeComponent();
			_storyboardEnableCurrent = (Storyboard)Resources["EnableCurrent"];
			_storyboardDisableCurrent = (Storyboard)Resources["DisableCurrent"];
		}
		public void Update()
		{
			var isActive = IsActive();

			if (isActive && _lastVisualState != LastVisualState.Active)
			{
				_lastVisualState = LastVisualState.Active;
				_storyboardDisableCurrent.Stop();
				_storyboardEnableCurrent.Begin();
			}
			else if (isActive == false && _lastVisualState != LastVisualState.Inactive)
			{
				_lastVisualState = LastVisualState.Inactive;
				_storyboardEnableCurrent.Stop();
				_storyboardDisableCurrent.Begin();
			}
		}
	}
}
