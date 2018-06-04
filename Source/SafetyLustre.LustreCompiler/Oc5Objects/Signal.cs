namespace SafetyLustre.LustreCompiler.Oc5Objects
{
    public abstract class Signal
    {
        public string Name { get; set; }

        public int VarIndex { get; set; }

        public int? BoolIndex { get; set; }
    }

    abstract class SingleSignal : Signal { }

    class SingleInputSignal : SingleSignal { }

    class SingleOutputSignal : SingleSignal { }
}
