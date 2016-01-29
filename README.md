Welcome to the S# Modeling Framework
============

The S# (pronounced "safety sharp") modeling framework provides safety engineers with a modeling language specifically designed to express important safety-related concepts such as _component faults_ and the _physical environment_ of a safety-critical 
system. For safety assessments, _model simulations_ as well as _formal safety analyses_ are supported. S# models are written in a 
domain specific language (DSL) that is embedded into the C# programming language with full access to all .NET 
libraries and tools. Even though S# models are _represented_ as code, they are still models, albeit 
executable ones; conceptually, they are at a higher level of abstraction than actual code. 

##Get Started
For the time being, S# is under heavy development. Documentation, unfortunately, is sparse. We plan on 
publishing a NuGet package and a Visual Studio extension, making it easy to install, update, and use S#. 

To play around with S# and the provided case studies:

1. Checkout the git repository: ``git checkout https://github.com/isse-augsburg/ssharp.git``
2. Open `SafetySharp.sln` with Visual Studio 2015.
3. Compile the solution.
4. Start the `Visualization` project. You can play around with visualizations of the case studies; try, for instance, right clicking that allows you to enable or disable faults. You can also debug counter examples once you have generated any.
5. Run the tests included in the individual case study projects. These conduct safety analyses or check arbitrary LTL formulas.

##Example
The following small sample shows the model of a pressure sensor using the S# modeling language. The sensor can 
be used to check whether a certain pressure level has been reached. The sample shows how safety-critical 
_components_ and their _required_ and _provided ports_ are declared using the C#-based DSL provided by S#. The 
model also includes a _fault_ that prevents the sensor from reporting that the pressure level has been 
reached, possibly resulting in a hazard at the system level.

```C#
/// Represents a model of a pressure sensor.
class PressureSensor : Component 
{
  /// The pressure level that the sensor reports.
  private readonly int _pressure;
  
  /// A persistent fault that can occur nondeterminisitcally; once it has occurred,
  /// it cannot disappear.
  private readonly Fault _noPressureFault = new PersistentFault();
  
  /// Instantiates an instance of a pressure sensor. The maximum allowed pressure is 
  /// passed in as a constructor argument, allowing for easy configuration and 
  /// re-use of component models.
  public PressureSensor(int pressure)
  {
      _pressure = pressure;
  }
  
  /// Required port. This is the port that the sensor uses to sense the actual 
  /// pressure level in some environment component.
  public extern int CheckPhysicalPressure();
  
  /// Provided port. Indicates whether the pressure level that the sensor is 
  /// configured to report has been reached.
  public bool HasPressureLevelBeenReached() 
  {
      return CheckPhysicalPressure() >= _pressure;
  }
  
  /// Represents the effect of the fault '_noPressureFault'.
  [FaultEffect(Fault = nameof(_noPressureFault)] 
  class SenseNoPressure : PressureTank
  { 
      /// Overwrites the behavior of the sensor's provided port, always returning the 
      /// constant 'false' when the fault is occurring.
      public bool HasPressureLevelBeenReached() 
      {
          return false; 
      }
  }
}
```
