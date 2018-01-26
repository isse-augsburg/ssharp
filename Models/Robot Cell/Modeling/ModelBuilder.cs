﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using SafetySharp.Modeling;
	using Odp;
	using Odp.Reconfiguration;

	using Controllers;
	using Controllers.Reconfiguration;
	using Controllers.Reconfiguration.PerformanceMeasurement;
	using Odp.Reconfiguration.CoalitionFormation;
	using Plants;
	using Resource = Controllers.Resource;

	internal partial class ModelBuilder
	{
		private readonly Model _model;

		private bool _usePlants = true;

		public ModelBuilder(string name = "")
		{
			_model = new Model(name);
		}

		public ModelBuilder DisablePlants()
		{
			_usePlants = false;
			return this;
		}

		public ModelBuilder DefineTask(int numWorkpieces = 10, params ICapability[] capabilities)
		{
			var task = new Task(capabilities);
			_model.Tasks.Add(task);
			_model.TaskQueue.Enqueue(task);

			for (var i = 0; i < numWorkpieces; ++i)
			{
				var workpiece = CreateWorkpiece(capabilities);
				_model.Resources.Add(new Resource(task, workpiece));
			}

			return this;
		}

		private Workpiece CreateWorkpiece(ICapability[] capabilities)
		{
			if (!_usePlants)
				return null;

			var name = $"W{_model.Workpieces.Count}";
			var workpiece = new Workpiece(capabilities.OfType<ProcessCapability>().Select(c => c.ProductionAction).ToArray())
			{
				Name = name,
				IncorrectlyPositionedFault = { Name = $"{name}.IncorrectlyPositioned" },
				ToolApplicationFailed = { Name = $"{name}.ToolApplicationFailed" }
			};

			_model.Workpieces.Add(workpiece);
			return workpiece;
		}

		public ModelBuilder AddRobot(params ICapability[] capabilities)
		{
			var robot = CreateRobot(capabilities);
			var agent = new RobotAgent(capabilities.Distinct().ToArray(), robot, _model.Tasks, _model.Resources) { TaskQueue = _model.TaskQueue };

			_model.RobotAgents.Add(agent);
			_model.ReconfigurationMonitor.AddAgent(agent);
			agent.ReconfigurationMonitor = _model.ReconfigurationMonitor;
			robot?.SetNames(agent.Id);

			return this;
		}

		private Robot CreateRobot(ICapability[] capabilities)
		{
			if (!_usePlants)
				return null;

			var robot = new Robot(capabilities.OfType<ProcessCapability>().ToArray());
			_model.Robots.Add(robot);
			return robot;
		}

		public ModelBuilder AddRobots(params ICapability[][] capabilitySets)
		{
			foreach (var capabilities in capabilitySets)
				AddRobot(capabilities);
			return this;
		}

		public ModelBuilder AddCart(params Tuple<int, int>[] routeSpecifications)
		{
			routeSpecifications = RouteHelper.ComputeRoutes(routeSpecifications);

			var cart = _usePlants ? CreateCart(RouteHelper.ToRoutes(routeSpecifications, _model)) : null;
			var agent = new CartAgent(cart) { TaskQueue = _model.TaskQueue };
			_model.CartAgents.Add(agent);
			cart?.SetNames(agent.Id);

			RouteHelper.Connect(agent, routeSpecifications, _model);

			return this;
		}

		private Cart CreateCart(Route[] routes)
		{
			if (!_usePlants)
				return null;

			var cart = new Cart(routes[0].Robot1, routes);
			_model.Carts.Add(cart);
			return cart;
		}

		public ModelBuilder AddCarts(params Tuple<int, int>[][] routeSets)
		{
			foreach (var routes in routeSets)
				AddCart(routes);
			return this;
		}

		public Model Build()
		{
			return _model;
		}

		public ModelBuilder ChooseController<T>(IConfigurationFinder finder) where T : IController
		{
			var component = finder as IComponent;
			if (component != null)
				_model.AdditionalComponents.Add(component);

			var controller = (IController)Activator.CreateInstance(typeof(T), Agents.ToArray(), finder);
			return ChooseController(controller);
		}

		public ModelBuilder ChooseController(IController controller)
		{
			_model.Controller = controller;

			var component = controller as IComponent;
			if (component != null)
				_model.AdditionalComponents.Add(component);

			return this;
		}

		public ModelBuilder EnableControllerVerification(bool verify)
		{
			if (verify)
				_model.Controller = new VerifyingController(_model.Controller);
			return this;
		}

	    public ModelBuilder EnablePerformanceMeasurement()
	    {
	        _model.Controller = new PerformanceMeasurementController(_model.Controller);
			foreach (var agent in Agents)
				agent.ReconfigurationStrategy = new PerformanceMeasurementReconfigurationStrategy(agent.ReconfigurationStrategy);
	        return this;
	    }

	    public ModelBuilder DisableIntolerableFaults()
	    {
	        _model.Faults.MakeNondeterministic();
	        IntolerableFaults().SuppressActivations();
	        return this;
        }

		public ModelBuilder TolerableFaultsAnalysis()
		{
			_model.Controller = new TolerableAnalysisController(_model.Controller);
		    return DisableIntolerableFaults();
		}

		public ModelBuilder IntolerableFaultsAnalysis()
		{
			_model.Controller = new IntolerableAnalysisController(_model.Controller);
			_model.Faults.MakeNondeterministic();
			TolerableFaults().SuppressActivations();
			_model.AdditionalComponents.Add((IComponent)_model.Controller);
			return this;
		}

		private IEnumerable<Fault> TolerableFaults()
		{
			return _model.Faults.Except(IntolerableFaults());
		}

		private IEnumerable<Fault> IntolerableFaults()
		{
			var agents = _model.CartAgents.Cast<Agent>().Concat(_model.RobotAgents);

			return agents.Select(ag => ag.ConfigurationUpdateFailed)
						 .Concat(_model.Workpieces.Select(w => w.IncorrectlyPositionedFault))
						 .Concat(_model.Workpieces.Select(w => w.ToolApplicationFailed))
						 .Concat(_model.Robots.Select(r => r.SwitchToWrongToolFault))
						 .Concat(_model.Carts.Select(c => c.Lost));
		}

		public ModelBuilder CentralReconfiguration()
		{
			foreach (var agent in Agents)
				agent.ReconfigurationStrategy = new CentralReconfiguration(_model.Controller);
			return this;
		}

	    public ModelBuilder UseControllerReconfigurationAgents()
	    {
	        foreach (var agent in Agents)
                agent.ReconfigurationStrategy = new ReconfigurationAgentHandler(agent, (b, h, t) => new ControllerReconfigurationAgent(b, h, _model.Controller));
	        return this;
	    }

	    public ModelBuilder UseCoalitionFormation(IConfigurationFinder finder)
	    {
	        ChooseController<CoalitionController>(finder);
	        foreach (var agent in Agents)
                agent.ReconfigurationStrategy = new ReconfigurationAgentHandler(agent, (b, h, t) => new CoalitionReconfigurationAgent(b, h, _model.Controller));
	        return this;
	    }

		private IEnumerable<Agent> Agents => _model.CartAgents.Cast<Agent>().Concat(_model.RobotAgents);
	}
}
