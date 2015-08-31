Welcome to the S# Modeling Framework
============

The S# (pronounced "safety sharp") modeling framework provides safety engineers with a modeling language specifically designed to express important safety-related concepts such as _component faults_ and the _physical environment_ of a safety-critical 
system. For safety assessments, _model simulations_ as well as _formal safety analyses_ are supported. S# models are written in a 
domain specific language (DSL) that is embedded into the C# programming language with full access to all .NET 
libraries and tools. S#'s own analysis tools are integrated into Visual Studio with state-of-the-art code editing 
and debugging features. Even though S# models are _represented_ as code, they are still models, albeit 
executable ones; conceptually, they are at a higher level of abstraction than actual code. 

##Get Started
For the time being, S# is under heavy development. Usage and installation instructions will be available once 
some technical details are sorted out and the code base is stabilized. We plan on 
publishing a NuGet package and a Visual Studio extension, making it easy to install, update, and use S#. 

##Example
The following small sample shows the model of a pressure sensor using the S# modeling language. The sensor can 
be used to check whether a certain pressure level has been reached. The sample shows how safety-critical 
_components_ and their _required_ and _provided ports_ are declared using the C#-based DSL provided by S#. The 
model also includes a _component fault_ that prevents the sensor from reporting that the pressure level has been 
reached, possibly resulting in a hazard at the system level.

```C#
/// Represents a model of a pressure sensor.
class PressureSensor : Component 
{
  /// The pressure level that the sensor reports.
  private readonly int _pressure;
  
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
  
  /// Represents a transient fault (i.e., a fault that can come and go at any time) 
  /// that prevents the sensor from reporting the pressure correctly.
  [Transient] 
  class SenseNoPressure : Fault
  { 
      /// Overwrites the behavior of the sensor's provided port, always returning the 
      /// constant 'false' for as long as the fault is active/occurring.
      public bool HasPressureLevelBeenReached() 
      {
          return false; 
      }
  }
}
```
