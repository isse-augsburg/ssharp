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

namespace ISSE.SafetyChecking.MinimalCriticalSetAnalysis
{
	using System;
	using System.Linq;
	using ExecutableModel;
	using Modeling;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Utilities;
	using Formula;

	/// <summary>
	///   Represents a back end for safety analyses, encapsulating the way that the individual checks are carried out on the
	///   analyzed model.
	/// </summary>
	internal abstract class AnalysisBackend<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		protected FaultSet ForcedFaults { get; private set; }
		public CoupledExecutableModelCreator<TExecutableModel> RuntimeModelCreator { get; private set; }
		protected FaultSet SuppressedFaults { get; private set; }

		/// <summary>
		///   Initizializes the model that should be analyzed.
		/// </summary>
		/// <param name="configuration">The configuration that should be used for the analyses.</param>
		/// <param name="createFreshModel">The creator for the model that should be checked.</param>
		/// <param name="hazard">The hazard that should be analyzed.</param>
		internal void InitializeModel(AnalysisConfiguration configuration, CoupledExecutableModelCreator<TExecutableModel> createFreshModel, Formula hazard)
		{
			RuntimeModelCreator = createFreshModel;
			ForcedFaults = new FaultSet(createFreshModel.FaultsInBaseModel.Where(fault => fault.Activation == Activation.Forced));
			SuppressedFaults = new FaultSet(createFreshModel.FaultsInBaseModel.Where(fault => fault.Activation == Activation.Suppressed));

			InitializeModel(configuration, hazard);
		}

		/// <summary>
		///   Initizializes the model that should be analyzed.
		/// </summary>
		/// <param name="configuration">The configuration that should be used for the analyses.</param>
		/// <param name="hazard">The hazard that should be analyzed.</param>
		protected abstract void InitializeModel(AnalysisConfiguration configuration, Formula hazard);

		/// <summary>
		///   Checks the <see cref="faults" /> for criticality using the <see cref="activation" /> mode.
		/// </summary>
		/// <param name="faults">The fault set that should be checked for criticality.</param>
		/// <param name="activation">The activation mode of the fault set.</param>
		internal abstract InvariantAnalysisResult CheckCriticality(FaultSet faults, Activation activation);

		/// <summary>
		///   Checks the order of <see cref="firstFault" /> and <see cref="secondFault" /> for the
		///   <see cref="minimalCriticalFaultSet" /> using the <see cref="activation" /> mode.
		/// </summary>
		/// <param name="firstFault">The first fault that should be checked.</param>
		/// <param name="secondFault">The second fault that should be checked.</param>
		/// <param name="minimalCriticalFaultSet">The minimal critical fault set that should be checked.</param>
		/// <param name="activation">The activation mode of the fault set.</param>
		/// <param name="forceSimultaneous">Indicates whether both faults must occur simultaneously.</param>
		internal abstract InvariantAnalysisResult CheckOrder(Fault firstFault, Fault secondFault, FaultSet minimalCriticalFaultSet,
													Activation activation, bool forceSimultaneous);

		/// <summary>
		///   Raised when output is generated during the analyses.
		/// </summary>
		internal event Action<string> OutputWritten;

		/// <summary>
		///   Raises the <see cref="OutputWritten" /> for the generated <paramref name="output" />.
		/// </summary>
		/// <param name="output">The output the event should be raised for.</param>
		protected void OnOutputWritten(string output)
		{
			OutputWritten?.Invoke(output);
		}

		/// <summary>
		///   Determines the effective <see cref="Activation" /> of the <paramref name="fault" /> when <paramref name="faults" /> should
		///   be checked for criticality.
		/// </summary>
		protected Activation GetEffectiveActivation(Fault fault, FaultSet faults, Activation activation)
		{
			Assert.That(fault != null && fault.IsUsed, "Invalid fault.");

			if (SuppressedFaults.Contains(fault))
				return Activation.Suppressed;

			if (ForcedFaults.Contains(fault))
				return Activation.Forced;

			if (faults.Contains(fault))
				return activation;

			return Activation.Suppressed;
		}
	}
}