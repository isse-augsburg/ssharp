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
	public partial class Pump : UserControl
	{
		private readonly Storyboard _storyboardActivate;
		private readonly Storyboard _storyboardDeactivate;
		private readonly Storyboard _storyboardPumping;

		private enum LastVisualState
		{
			Unset,
			Active,
			Inactive
		}

		public Func<bool> IsPowered { get; set; }
		public Func<bool> IsPumping { get; set; }

		private LastVisualState _lastVisualStateIsPumping;
		private LastVisualState _lastVisualStateIsPowered;

		public Pump()
		{
			_lastVisualStateIsPumping = LastVisualState.Unset;
			_lastVisualStateIsPowered = LastVisualState.Unset;
			InitializeComponent();
			_storyboardActivate = (Storyboard)Resources["GettingActivatedStoryBoard"];
			_storyboardDeactivate = (Storyboard)Resources["GettingDeactivatedStoryBoard"];
			_storyboardPumping = (Storyboard)Resources["PumpingStoryBoard"];
		}

		public void Update()
		{
			var isPumping = IsPumping();
			if (isPumping && _lastVisualStateIsPumping != LastVisualState.Active)
			{
				_lastVisualStateIsPumping = LastVisualState.Active;
				_storyboardPumping.RepeatBehavior = RepeatBehavior.Forever;
				_storyboardPumping.Begin();
			}
			else if (isPumping == false && _lastVisualStateIsPumping != LastVisualState.Inactive)
			{
				_lastVisualStateIsPumping = LastVisualState.Inactive;
				_storyboardPumping.Stop();
			}
			
			var isPowered = IsPowered();
			if (isPowered && _lastVisualStateIsPowered != LastVisualState.Active)
			{
				_lastVisualStateIsPowered = LastVisualState.Active;
				_storyboardDeactivate.Stop();
				_storyboardActivate.Begin();
			}
			else if (isPowered == false && _lastVisualStateIsPowered != LastVisualState.Inactive)
			{
				_lastVisualStateIsPowered = LastVisualState.Inactive;
				_storyboardActivate.Stop();
				_storyboardDeactivate.Begin();
			}

		}
	}
}
