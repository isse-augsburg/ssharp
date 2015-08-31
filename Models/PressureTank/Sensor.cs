// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace PressureTank
{
	using SafetySharp.Modeling;
	using SafetySharp.Modeling.Faults;

	/// <summary>
	///   Represents the sensor that monitors the pressure within the pressure tank.
	/// </summary>
	public class Sensor : Component
	{
		/// <summary>
		///   The pressure level the sensor is watching for.
		/// </summary>
		private readonly int _triggerPressure;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="triggerPressure">The pressure level the sensor should be watching for.</param>
		public Sensor(int triggerPressure)
		{
			_triggerPressure = triggerPressure;
		}

		//PersistentFault f2 = new PersistentFault(new SuppressIsEmpty(), new SuppressIsFull());

		/// <summary>
		///   Senses the physical pressure level within the tank.
		/// </summary>
		// TODO: Consider using a property once supported by S#.
		public extern int CheckPhysicalPressure();

		/// <summary>
		///   Gets a value indicating whether the triggering pressure level has been reached or exceeded.
		/// </summary>
		// TODO: Consider using a property once supported by S#.
		public bool IsFull() => CheckPhysicalPressure() >= _triggerPressure;

		/// <summary>
		///   Gets a value indicating whether the tank is empty.
		/// </summary>
		// TODO: Consider using a property once supported by S#.
		public bool IsEmpty() => CheckPhysicalPressure() <= 0;

		/// <summary>
		///   Represents a failure mode that prevents the sensor from triggering when the tank has reached or exceeded its
		///   maximum allowed pressure level.
		/// </summary>
		[Transient]
		public class SuppressIsFull : Fault
		{
			public bool IsFull() => false;
		}

		/// <summary>
		///   Represents a failure mode that prevents the sensor from triggering when the tank has become empty.
		/// </summary>
		[Transient]
		public class SuppressIsEmpty : Fault
		{
			public bool IsEmpty() => false;
		}
	}
}

//public class FaultEffect
//{
//	static public object Create<T>(string n, T t)
//	{
//		return null;
//	}
//}
//
//class Fault2<T>
//{
//	public Fault2(params FaultEffect[] f)
//	{
//		
//	}
//
//	public OccurrencePattern OccurrencePattern { get; set; }
//	public List<object> FaultsEffects { get; set; } = new List<object>();
//	public Dictionary<string, object> FaultsEffectsD { get; set; } = new Dictionary<string, object>();
//
//}
//
//class PersistentFault : Fault2<Persistent>
//{
//	public PersistentFault(params FaultEffect[] f)
//		: base(f)
//	{
//	}
//}
