using System.Linq;
using System.Windows.Controls;
using SafetySharp.CaseStudies.RobotCell.Modeling.Controllers;
using System.Windows;
using SafetySharp.Modeling;
using System.Windows.Shapes;

namespace SafetySharp.CaseStudies.Visualizations
{
    using CaseStudies.RobotCell.Modeling.Plants;
    using System;
    using System.Collections.Generic;
    using System.Windows.Media;

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

            DisplayCartRoutes();
            //PrintCartRoutes();

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

        public void DisplayCartRoutes()
        {
            List<string> routes = new List<string>();
            var routesString = "";

            foreach (var input in _cartAgent.Inputs)
            {
                if (!routes.Contains((input.Id + 1).ToString()))
                    routes.Add((input.Id + 1).ToString());
            }

            foreach (var route in routes)
            {
                routesString += route + ", ";
            }

            if (routesString.Length > 2)
                routesTxt.Text = routesString.Remove(routesString.Length - 2, 2);
            else
                routesTxt.Text = routesString;
        }

        public void PrintCartRoutes()
        {
            var routes = _cartAgent.Cart.Routes;
            int i = 0;

            routesTxt.Text = "";
            Console.WriteLine();

            foreach (var route in routes)
            {
                var nameR1 = route.Robot1.Name;
                var nameR2 = route.Robot2.Name;
                Console.WriteLine("Route {0} for the cart with the id " + _cartAgent.Id + " is: " + nameR1 + " to " + nameR2, i + 1);
                routesTxt.Text += CreateRouteString(nameR1, nameR2);
                i++;
            }
            Console.WriteLine("Final routesTxt string: " + routesTxt.Text);
        }

        public string CreateRouteString(string r1, string r2)
        {
            return "(" + r1+ "," + r2 + ") ";
        }
    }
}
