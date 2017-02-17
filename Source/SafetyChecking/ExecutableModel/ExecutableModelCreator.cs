// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.ExecutableModel
{
	using System;
	using Formula;
	using Modeling;


	public class CoupledExecutableModelCreator<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		// Coupled means that the field "Formulas" of the TExecutableModel instantiated by Create is the equivalent of CoupledFormulas.

		// Create an instance
		// Note: If a model created by the returned delegate is used, old "stateFormulas" cannot be used anymore, because they
		// might have parts of the "old" model (the one for which formulas have been instantiated in) in their closure.
		// Use "model.Formulas" instead! The order should be preserved.
		// The parameter of Create are the stateHeaderBytes (the number of reserved bytes at the beginning of each state vector)
		public Func<int,TExecutableModel> Create { get; }

		// Contains the source model. Depending on TExecutableModel, it might be a class instance or a string to a file name, or whatever.
		// Code in ISSE.SafetyChecking does not depend on it.
		public object SourceModel{ get; }


		// Note: FaultsInBaseModel are coupled to the model instance this.ModelBase and not the (future) instance created by this.Create().
		// A transfer might be necessary!
		public Fault[] FaultsInBaseModel;

		// Note: FormulasToCheckInBaseModel are coupled to the model instance this.ModelBase and not the (future) instance created by this.Create().
		// A transfer with TransferFormulaToNewExecutedModelInstanceVisitor might be necessary!
		public Formula[] StateFormulasToCheckInBaseModel;

		public CoupledExecutableModelCreator(Func<int, TExecutableModel> creator, object sourceModel, Formula[] stateFormulasToCheckInBaseModel, Fault[] faultsInBaseModel)
		{
			Create = creator;
			SourceModel = sourceModel;
			StateFormulasToCheckInBaseModel = stateFormulasToCheckInBaseModel;
			FaultsInBaseModel = faultsInBaseModel;
		}
	}

	public class ExecutableModelCreator<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		// "Uncoupled" means that the field "Formulas" (StateFormulasToCheck) of the TExecutableModel are set by the parameter of the creator
		
		// Note: If a model created by the returned delegate is used, old "stateFormulas" cannot be used anymore, because they
		// might have parts of the "old" model (the one for which formulas have been instantiated in) in their closure.
		// Use "model.Formulas" instead! The order should be preserved.
		public Func<Formula[], CoupledExecutableModelCreator<TExecutableModel>> CreateCoupledModelCreator { get; }

		// Create a model creator which is coupled to the parameter formulasToCheckInBaseModel
		public CoupledExecutableModelCreator<TExecutableModel> Create(Formula[] formulasToCheckInBaseModel)
		{
			return CreateCoupledModelCreator(formulasToCheckInBaseModel);
		}

		// Contains the source model. Depending on TExecutableModel, it might be a class instance or a string to a file name, or whatever.
		// Code in ISSE.SafetyChecking does not depend on it.
		public object SourceModel { get; private set; }


		public ExecutableModelCreator(Func<Formula[], CoupledExecutableModelCreator<TExecutableModel>> creator, object sourceModel)
		{
			CreateCoupledModelCreator = creator;
			SourceModel = sourceModel;
		}
	}
}
