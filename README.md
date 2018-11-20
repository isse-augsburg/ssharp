Welcome to S#
============

S# (pronounced "safety sharp") is a formal modeling framework and safety analysis
framework for safety-critical systems developed by the Institute for Software & Systems
Engineering at the University of Augsburg.

__S# Features__
- Expressive, modular modeling language based on the C# programming language
- Fully automated and efficient formal safety analyses
- Efficient formal analyses using fully exhaustive model checking
- Support for model simulations, model tests, model debugging, and model visualizations
- Extensive tool support based on standard .NET tools and libraries such as Visual Studio,
  providing model refactorings, debuggers, UI designers for visualizations, continuous
  integration with automated regression tests, etc.

Download S# and the Case Studies to Get Started
-------------------------

To get started with S#, please consult the [Wiki](http://safetysharp.isse.de/wiki). S# and
the case studies are available under the 
[MIT License](https://github.com/isse-augsburg/ssharp/blob/master/LICENSE.md).

Links
-------------------------

- [(Partial) reimplementation in C++](https://github.com/joleuger/pemc/)

- [Wiki page with our S# related publications](https://github.com/isse-augsburg/ssharp/wiki/Publications)

- [Quickstart S# for developers (Visual Studio 2017)](https://github.com/isse-augsburg/ssharp/blob/master/Documents/Quickstart-Dev-VS2017.pdf)

- [Review S# Case Studies (Visual Studio 2015)](https://github.com/isse-augsburg/ssharp/wiki/Installation-and-Setup#review-ss-case-studies)

- [Model and conduct DCCA on your own Case Studies with S# (Visual Studio 2015)](https://github.com/isse-augsburg/ssharp/wiki/Installation-and-Setup#model-and-analyze-your-own-case-studies-with-s)




Example: Modeling with S#
-------------------------

The following small and incomplete example shows the model of a [pressure
sensor](https://github.com/isse-augsburg/ssharp/wiki/Pressure%20Tank%20Case%20Study) using
the S#'s modeling language: The sensor checks whether a certain pressure level is reached.
The example shows how safety-critical _components_ and their _required_ and _provided
ports_ are modeled in S#. The model also includes a _fault_ that prevents the sensor from
reporting that the pressure level is reached, possibly resulting in a hazard at the system
level. For more details about modeling with S#, please consult the [Wiki](https://github.com/isse-augsburg/ssharp/wiki/Components).

```csharp
// Represents a model of a pressure sensor.
class PressureSensor : Component
{
  // The pressure level that the sensor reports.
  private readonly int _pressure;

  // A persistent fault that can occur nondeterminisitcally; once it has occurred,
  // it cannot disappear.
  private readonly Fault _noPressureFault = new PermanentFault();

  // Instantiates an instance of a pressure sensor. The maximum allowed pressure is
  // passed in as a constructor argument, allowing for easy configuration and
  // re-use of component models.
  public PressureSensor(int pressure)
  {
      _pressure = pressure;
  }

  // Required port. This is the port that the sensor uses to sense the actual
  // pressure level in some environment component.
  public extern int CheckPhysicalPressure();

  // Provided port. Indicates whether the pressure level that the sensor is
  // configured to report has been reached.
  public virtual bool HasPressureLevelBeenReached()
  {
      return CheckPhysicalPressure() >= _pressure;
  }

  // Represents the effect of the fault '_noPressureFault'.
  [FaultEffect(Fault = nameof(_noPressureFault)]
  class SenseNoPressure : PressureTank
  {
      // Overwrites the behavior of the sensor's provided port, always returning the
      // constant 'false' when the fault is activated.
      public override bool HasPressureLevelBeenReached()
      {
          return false;
      }
  }
}
```

Example: Safety Analysis with S#
-------------------------

To conduct fully automated safety analyses with S#, the following simple code is required.
Analysis results are shown for the [pressure tank case
study](https://github.com/isse-augsburg/ssharp/wiki/Pressure%20Tank%20Case%20Study); for
more details, please see the
[Wiki](https://github.com/isse-augsburg/ssharp/wiki/Safety%20Analysis).

```csharp
var result = SafetyAnalysis.AnalyzeHazard(model, hazard);

result.SaveCounterExamples("counter examples");
Console.WriteLine(result);
```

```
=======================================================================
=======      Deductive Cause Consequence Analysis: Results      =======
=======================================================================

Elapsed Time: 00:00:02.1703065
Fault Count: 4
Faults: SuppressIsEmpty, SuppressIsFull, SuppressPumping, SuppressTimeout

Checked Fault Sets: 13 (81% of all fault sets)
Minimal Critical Sets: 1

   (1) { SuppressIsFull, SuppressTimeout }
```
