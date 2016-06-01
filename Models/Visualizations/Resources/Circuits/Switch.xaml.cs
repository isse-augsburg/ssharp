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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SafetySharp.CaseStudies.Visualizations.Resources.Circuits
{
	using System.Windows.Media.Animation;

	/// <summary>
	/// Interaktionslogik für LoadCircuitContact.xaml
	/// </summary>
	public partial class Switch : UserControl
	{
		private readonly Storyboard _storyboardClose;
		private readonly Storyboard _storyboardOpen;

		private enum LastVisualState
		{
			Unset,
			Pushed,
			NotPushed
		}

		private LastVisualState _lastVisualState;

		public Func<bool> IsPushed { get; set; }

		public Switch()
		{
			_lastVisualState = LastVisualState.Unset;
			InitializeComponent();
			_storyboardClose = (Storyboard)Resources["CloseCircuitStoryBoard"];
			_storyboardOpen = (Storyboard)Resources["OpenCircuitStoryBoard"];
		}
		public void Update()
		{
			var isPushed = IsPushed();

			if (isPushed && _lastVisualState != LastVisualState.Pushed)
			{
				_lastVisualState = LastVisualState.Pushed;
				_storyboardOpen.Stop();
				_storyboardClose.Begin();
			}
			else if (isPushed == false && _lastVisualState != LastVisualState.NotPushed)
			{
				_lastVisualState = LastVisualState.NotPushed;
				_storyboardClose.Stop();
				_storyboardOpen.Begin();
			}
		}
	}
}
