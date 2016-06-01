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
	public partial class NamedElement : UserControl
	{
		private readonly Storyboard _storyboardActivate;
		private readonly Storyboard _storyboardDeactivate;

		private enum LastVisualState
		{
			Unset,
			Active,
			Inactive
		}

		private LastVisualState _lastVisualState;

		public Func<bool> IsActive { get; set; }

		public NamedElement()
		{
			_lastVisualState = LastVisualState.Unset;
			InitializeComponent();
			_storyboardActivate = (Storyboard)Resources["GettingActivatedStoryBoard"];
			_storyboardDeactivate = (Storyboard)Resources["GettingDeactivatedStoryBoard"];
		}

		/// <summary>
		/// Gets or sets the NameOfElement
		/// </summary>
		public string NameOfElement
		{
			get { return (string)GetValue(NameOfElementProperty); }
			set { SetValue(NameOfElementProperty, value); }
		}

		/// <summary>
		/// Identified the NameOfElement dependency property
		/// </summary>
		public static readonly DependencyProperty NameOfElementProperty =
			DependencyProperty.Register("NameOfElement", typeof(string),
			  typeof(NamedElement), new PropertyMetadata("?"));

		public void Update()
		{
			var isActive = IsActive();

			if (isActive && _lastVisualState != LastVisualState.Active)
			{
				_lastVisualState = LastVisualState.Active;
				_storyboardDeactivate.Stop();
				_storyboardActivate.Begin();
			}
			else if (isActive == false && _lastVisualState != LastVisualState.Inactive)
			{
				_lastVisualState = LastVisualState.Inactive;
				_storyboardActivate.Stop();
				_storyboardDeactivate.Begin();
			}
		}
	}
}
