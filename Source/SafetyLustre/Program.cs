// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Manuel Götz
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ISSE.SafetyChecking;
using ISSE.SafetyChecking.Formula;
using Tests.SimpleExecutableModel;
using System.Globalization;

namespace BachelorarbeitLustre {
    public class Program {

        public static bool modelChecking = false;

        public List<Object> output;
        public int state;
        public Queue<Object>[] input;

        public List<Constant> constants;
        public List<Variable> variables;
        public List<Signal> signals;
        public List<Action> actions;
        public List<State> states;

        static Regex atom = new Regex("^#[-]?[1-9]*[0-9]$");
        static Regex atom_double = new Regex("^#[-]?[1-9]*[0-9].[0-9]+$");
        static Regex constant = new Regex("^@[1-9]*[0-9]$");
        static Regex constant_predefined = new Regex("^@\\$[0-1]$");
        static Regex variable = new Regex("^[1-9]*[0-9]$");
        static Regex function = new Regex("^\\$[1-9]*[0-9]");

        const string cygwinPath = "c:\\cygwin64\\bin\\lustre\\bin";
        public static string ocExaplesPath = "";
		public static string ocExaplesPathSuffix = "\\Examples\\";

		public Program(string fileName, string mainNode) {
            //Compiling Lustre-Program
            if (mainNode != null) {
                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "cmd.exe";
                info.RedirectStandardInput = true;
                info.UseShellExecute = false;

                p.StartInfo = info;
                p.Start();

                using (StreamWriter sw = p.StandardInput) {
                    if (sw.BaseStream.CanWrite) {
                        sw.WriteLine("c:");
                        sw.WriteLine("cd " + cygwinPath);
                        sw.WriteLine("sh lus2oc " + ocExaplesPath + fileName + ".lus " + mainNode + " -o " + ocExaplesPath + fileName + ".oc");
                    }
                }
                Console.WriteLine("Compiling your .lus file to .oc file.\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }

            output = new List<object>();
            constants = new List<Constant>();
            variables = new List<Variable>();
            signals = new List<Signal>();
            actions = new List<Action>();
            states = new List<State>();

            new Parser(ocExaplesPath + fileName + ".oc", this);
        }

        private void executeProgram() {
            if (input.Length != 0) {
                int length = input[0].Count;
                for (int i = 1; i < input.Length; i++) {
                    if (input[i].Count != length) {
                        throw new Exception("All input signals have to have same length");
                    }
                }
            }

            while (state != -1 && (input.Length > 0) ? (input[0].Count > 0) : true) {
                executeProcedure();
            }
            Console.WriteLine("\nPress any key to end...");
            Console.ReadKey();
        }

        public void executeProcedure() {
            try {
                State activeState;
                output.Clear();
                if (state < states.Count) {
                    activeState = states[state];
                }
                else {
                    throw new SyntaxException("Syntax error: state " + state + " does not exist");
                }
                var activeNode = activeState.getRoot();
                for (int i = 0; ; i++) {
                    if (actions[i].getAction() != 0) {
                        break;
                    }
                    if (signals.Count > actions[i].getIndex() && signals[actions[i].getIndex()].getNature() == 0) {
                        signals[actions[i].getIndex()].getVariable().setValue(input[i].Dequeue());
                    }
                }
                while (true) {
                    if (activeNode.getState() == -1) {
                        if (activeNode.executeAction()) {
                            activeNode = activeNode.getLeft();
                        }
                        else {
                            if (activeNode.getRight() == null) {
                                throw new SyntaxException("Error while executing action " + activeNode.getAction().getId());
                            }
                            activeNode = activeNode.getRight();
                        }
                    }
                    else {
                        if (activeNode.getState() < states.Count) {
                            state = activeNode.getState();
                            break;
                        }
                        else {
                            throw new SyntaxException("Syntax error: state " + activeNode.getState() + " does not exist");
                        }

                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine("Error executing the program:");
                Console.WriteLine(e.Message);
            }
        }

        public Object executeExpression(string expression) {
            Object y = null;
            Object z = null;
            if (expression.StartsWith("(") && expression.EndsWith(")")) {
                expression = expression.Substring(1, expression.Length - 2);
            }
            if (function.IsMatch(expression)) {
                int action;
                int bracket = 0;
                int i;
                if (!Int32.TryParse(expression.Substring(1, expression.IndexOf('(') - 1), out action)) {
                    throw new SyntaxException("Syntax error in expression \"" + expression + "\"");
                }
                expression = expression.Substring(expression.IndexOf('(') + 1);
                expression = expression.Substring(0, expression.Length - 1);
                //Object 1
                for (i = 0; i < expression.Length; i++) {
                    if (expression.ElementAt(i) == '(') {
                        bracket++;
                    }
                    if (expression.ElementAt(i) == ')') {
                        bracket--;
                    }
                    if (expression.ElementAt(i) == ',' && bracket == 0) {
                        break;
                    }
                }
                Object x = executeExpression(expression.Substring(0, i));
                expression = expression.Substring(Math.Min(i + 1, expression.Length));
                if (expression.Length != 0) {
                    //Object 2
                    bracket = 0;
                    for (i = 0; i < expression.Length; i++) {
                        if (expression.ElementAt(i) == '(') {
                            bracket++;
                        }
                        if (expression.ElementAt(i) == ')') {
                            bracket--;
                        }
                        if (expression.ElementAt(i) == ',' && bracket == 0) {
                            break;
                        }
                    }
                    y = executeExpression(expression.Substring(0, i));
                    expression = expression.Substring(Math.Min(i + 1, expression.Length));
                }
                if (expression.Length != 0) {
                    //Object 3
                    bracket = 0;
                    for (i = 0; i < expression.Length; i++) {
                        if (expression.ElementAt(i) == '(') {
                            bracket++;
                        }
                        if (expression.ElementAt(i) == ')') {
                            bracket--;
                        }
                        if (expression.ElementAt(i) == ',' && bracket == 0) {
                            break;
                        }
                    }
                    z = executeExpression(expression.Substring(0, i));
                }
                return PredefinedObjects.executeFunction(action, x, y, z);
            }
            else if (atom.IsMatch(expression)) {
                if (expression.StartsWith("-")) {
                    return -Int32.Parse(expression.Substring(1, expression.Length - 1));
                }
                else {
                    return Int32.Parse(expression.Substring(1, expression.Length - 1));
                }
            }
            else if (atom_double.IsMatch(expression)) {
                if (expression.StartsWith("-")) {
                    return -Double.Parse(expression.Substring(1, expression.Length - 1), CultureInfo.InvariantCulture);
                }
                else {
                    return Double.Parse(expression.Substring(1, expression.Length - 1), CultureInfo.InvariantCulture);
                }

            }
            else if (constant.IsMatch(expression)) {
                if (constants.Count <= Int32.Parse(expression.Substring(1))) {
                    throw new SyntaxException("The constant @" + Int32.Parse(expression.Substring(1)) + " can not be referenced");
                }
                return constants[Int32.Parse(expression.Substring(1))].getValue();
            }
            else if (constant_predefined.IsMatch(expression)) {
                if (expression.Substring(2).Equals("0")) {
                    return false;
                }
                else {
                    return true;
                }
            }
            else if (variable.IsMatch(expression)) {
                if (variables.Count <= Int32.Parse(expression)) {
                    throw new SyntaxException("The variable " + Int32.Parse(expression.Substring(1)) + " can not be referenced");
                }
                return variables[Int32.Parse(expression)].getValue();
            }
            else {
                throw new SyntaxException("Syntax error in expression \"" + expression + "\"");
            }
        }

        public int countVariables(int type) {
            if (!(type >= 0 && type < 5)) {
                return 0;
            }
            int count = 0;
            foreach (var v in variables) {
                if (v.getType() == type) {
                    count++;
                }
            }
            return count;
        }

        static void Main(string[] args)
        {
	        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var path = new FileInfo(assemblyLocation).DirectoryName;
			ocExaplesPath = path + ocExaplesPathSuffix;
            Console.WriteLine("Select Mode:\n(1)Model Checking\n(2)Run Pressure Tank\n(3)Create OC-file");
            var key = Console.ReadKey();
            Console.WriteLine("");
            if (key.Key.Equals(ConsoleKey.D1) || key.Key.Equals(ConsoleKey.NumPad1)) {
                Formula invariant = new LustrePressureBelowThreshold();
                var modelChecker = new LustreQualitativeChecker("pressureTank", invariant);

                var result = modelChecker.CheckInvariant(invariant, 100);
                Console.WriteLine("Checked formula: pressure < " + LustrePressureBelowThreshold.threshold);
                Console.WriteLine("Formula holds: " + result.FormulaHolds);
                if (result.CounterExample != null) {
                    Console.WriteLine("\nStates until the formula is violated:");
                    for (int i = 0; i < result.CounterExample.States.Length; i++) {
                        for (int x = 0; x < result.CounterExample.States[i].Length; x++) {
                            if (x != 0)
                            {
                                Console.Write("|");
                            }
                            if (result.CounterExample.States[i][x] < 10)
                            {
                                Console.Write(" " + result.CounterExample.States[i][x]);
                            }
                            else
                            {
                                Console.Write(result.CounterExample.States[i][x]);
                            }
                        }
                        Console.Write("\n");
                    }
                }

                Console.WriteLine("\nPress any key to end...");
                Console.ReadKey();
            }
            else if (key.Key.Equals(ConsoleKey.D2) || key.Key.Equals(ConsoleKey.NumPad2)) {
                const string fileName = "pressureTank";
                const string mainNode = "TANK";

                var p = new Program(fileName, mainNode);
                p.input = new Queue<Object>[] { new Queue<Object>(new Object[] { false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }) };


                p.executeProgram();
            }
            else if (key.Key.Equals(ConsoleKey.D3) || key.Key.Equals(ConsoleKey.NumPad3)) {
                const string fileName = "ex2";
                const string mainNode = "EDGE";

                var p = new Program(fileName, mainNode);
            }
        }
    }
}
