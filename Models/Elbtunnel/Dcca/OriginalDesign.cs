
namespace Elbtunnel.Dcca
{
	using System;
	using System.Runtime.CompilerServices;
	using FluentAssertions;
	using System.Linq;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Runtime;
	using SafetySharp.Simulation;
	using Sensors;
	using SharedComponents;

	[TestFixture]
	public class OriginalDesignDcca
	{
		private class Model : Design1Original
		{
			LtlFormula EmptySet()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}

			LtlFormula PreLightBarrier_LightBarrier_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___PreLightBarrier_LightBarrier_FalseDetection()
			{
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___MainLightBarrier_LightBarrier_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___MainLightBarrier_LightBarrier_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___MainLightBarrier_LightBarrier_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___MainLightBarrier_LightBarrier_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___MainLightBarrier_LightBarrier_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_Misdetection___MainDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_Misdetection___MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_FalseDetection___MainDetectorRight_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_FalseDetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
			LtlFormula EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula PreLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula MainLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorLeft_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula MainDetectorRight_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				EndDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();

				return GetHazard();
			}
			LtlFormula EndDetectorLeft_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
			{
				PreLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				PreLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.Misdetection>();
				MainLightBarrier.IgnoreFault<LightBarrier.FalseDetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorLeft.IgnoreFault<OverheadDetector.FalseDetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.Misdetection>();
				MainDetectorRight.IgnoreFault<OverheadDetector.FalseDetection>();

				return GetHazard();
			}
		}

		private readonly Model _model;
		private readonly LtsMin _ltsMin;

		public OriginalDesignDcca()
		{
			_model = new Model();
			_ltsMin = new LtsMin(_model);
		}

		private void Check([CallerMemberName] string factory = null)
		{
			_ltsMin.CheckInvariant(factory).Should().BeTrue();
		}

		[Test]
		public void ListFaults()
		{
			_model.GetMetadata().RootComponent.VisitPreOrder(component =>
			{
				if (component.Faults.Length == 0)
					return;

				Console.WriteLine(String.Join(".", component.GetPath()));
				foreach (var fault in component.Faults)
					Console.WriteLine("\t{0}", fault.Name);
			});
		}

		[Test]
		public void EmptySet()
		{
			Check();
		}

		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___PreLightBarrier_LightBarrier_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___MainLightBarrier_LightBarrier_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___MainLightBarrier_LightBarrier_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___MainLightBarrier_LightBarrier_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___MainLightBarrier_LightBarrier_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___MainLightBarrier_LightBarrier_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___MainDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection___MainDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_Misdetection___MainDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_Misdetection___MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_FalseDetection___MainDetectorRight_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_FalseDetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_Misdetection___MainDetectorRight_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_Misdetection()
		{
			Check();
		}
		[Test]
		public void EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void PreLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainLightBarrier_LightBarrier_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorLeft_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void MainDetectorRight_OverheadDetector_FalseDetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
		[Test]
		public void EndDetectorLeft_OverheadDetector_Misdetection___EndDetectorLeft_OverheadDetector_FalseDetection()
		{
			Check();
		}
	}
}

