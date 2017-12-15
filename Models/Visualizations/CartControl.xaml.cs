using System.Linq;
using System.Windows.Controls;
using SafetySharp.CaseStudies.RobotCell.Modeling.Controllers;
using System.Windows;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.Visualizations
{ 
    /// <summary>
    /// Interaktionslogik für CartControl.xaml
    /// </summary>
    public partial class CartControl
    {
        private CartAgent _cartAgent;
        private readonly RobotCell _container;
        private uint _positioned;

        public CartControl(CartAgent cartAgent, RobotCell container)
        {
            _container = container;
            InitializeComponent();
            Update(cartAgent);
        }

        public void Update(CartAgent cart) {
            _cartAgent = cart;
            state.Text = RobotCell.GetState(cart);

            var newPosition = GetPosition();
            if (_positioned != newPosition.Id) {
                MoveTo(newPosition);
                _positioned = newPosition.Id;
            }

            InvalidateArrange();
            InvalidateVisual();
            UpdateLayout();
        }

        private RobotAgent GetPosition() {
            return _container.Model.RobotAgents.First(r => _cartAgent.Cart.IsPositionedAt(r.Robot));
        }

        private void MoveTo(RobotAgent destination) {
            int row, column;
            _container.GetFreePosition(destination, out row, out column);

            //Adjusting cart position, to be fixed otherwise
            row += 2; column += 2;

            Grid.SetRow(this, row);
            Grid.SetColumn(this, column);
        }

        private void OnBroken(object sender, RoutedEventArgs e) {
            //to-do, currently just for the first cart of the Carts-list
            _container.Model.Carts.First().Broken.ToggleActivationMode();
        }

        public CartAgent GetCartAgent() {
            return _cartAgent;
        }
    }
}
