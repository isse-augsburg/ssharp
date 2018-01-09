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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;
using Xunit.Sdk;

namespace BachelorarbeitLustre {
    public class Parser {

        static Regex id = new Regex("^[1-9]*[0-9]:$");
        static Regex type = new Regex("^\\$[0-4]$");
        static Regex signal_nature_name = new Regex("^(input|output):[a-zA-Z_-]+$");
        static Regex signal_actionindex = new Regex("^-|([1-9]*[0-9])$");
        static Regex signal_channel = new Regex("^(pure|single|multiple):[1-9]*[0-9]$");
        static Regex signal_bool = new Regex("^bool:[1-9]*[0-9]$");
        static Regex action_a = new Regex("^(present|dsz|output|reset|act|goto):$");
        static Regex action_b = new Regex("^if:$");
        static Regex action_c = new Regex("^(call|combine):[$][0-4]$");
        static Regex action_variable_index = new Regex("^\\([1-9]*[0-9]\\)$");
        static Regex state_jump = new Regex("^<[1-9]*[0-9]>");
        static Regex state_number = new Regex("^[1-9]*[0-9]$");
        static Regex state_number_bracket = new Regex("^[1-9]*[0-9]");

        private Program program;

        public Parser(string fileName, Program program)
        {
            this.program = program;
            readFile(fileName);
        }

        private void readFile(string fileName) {
            try {
                using (StreamReader sr = new StreamReader(fileName)) {
                    int linecounter = 1;
                    string line;
                    string[] splitline;

                    //oc5
                    line = sr.ReadLine();
                    if (line == null || !line.Equals("oc5:")) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"oc5:\" expected");
                    }
                    //module: foo
                    linecounter++;
                    line = sr.ReadLine();
                    if (line == null || !line.Split(' ')[0].Equals("module:")) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"module:\" expected");
                    }
                    if (line.Split(' ').Length != 2) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": Wrong module name");
                    }
                    bool tables = true;
                    int x;
                    int count;
                    while (tables) {
                        //newline
                        linecounter++;
                        line = sr.ReadLine();
                        if (line == null || !line.Equals("")) {
                            throw new SyntaxException("Syntax error in line " + linecounter + ": newline expected");
                        }
                        //tables
                        linecounter++;
                        line = sr.ReadLine();
                        if (line == null || line.Split(' ').Length != 2) {
                            throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                        }
                        else {
                            switch (line.Split(' ')[0]) {
                                case "types:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    throw new SyntaxException("Error: table \"types\" is not supported");
                                case "constants:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    for (int i = 0; i < count; i++) {
                                        linecounter++;
                                        line = sr.ReadLine();
                                        if (line == null) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                                        }
                                        splitline = line.TrimStart().Split(' ');
                                        if (splitline.Length != 3 && splitline.Length != 5) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": constant definition expected");
                                        }
                                        if (!id.IsMatch(splitline[0]) || !Int32.TryParse(splitline[0].Remove(splitline[0].Length - 1), out x) || x != i) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong constant index. \"<id>:\" expected");
                                        }
                                        if (!type.IsMatch(splitline[2])) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong type index. \"$<type>\" expected");
                                        }
                                        if (splitline.Length == 3) {
                                            program.constants.Add(new Constant(x, splitline[1], Int32.Parse(splitline[2].Last().ToString())));
                                        }
                                        else if (splitline[3].Equals("value:")) {
                                            try {
                                                program.constants.Add(new Constant(x, splitline[1], Int32.Parse(splitline[2].Last().ToString()), splitline[4]));
                                            }
                                            catch (Exception) {
                                                throw new SyntaxException("Syntax error in line " + linecounter + ": value is not from same type as constant");
                                            }
                                        }
                                        else {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": \"value:\" expected");
                                        }

                                    }
                                    //end:
                                    linecounter++;
                                    line = sr.ReadLine();
                                    if (line == null || !(line.Equals("end: ") || line.Equals("end:"))) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"end:\" expected");
                                    }
                                    break;
                                case "functions:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    throw new SyntaxException("Error: table \"functions\" is not supported");
                                case "procedures:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    throw new SyntaxException("Error: table \"procedures\" is not supported");
                                case "signals:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    for (int i = 0; i < count; i++) {
                                        linecounter++;
                                        line = sr.ReadLine();
                                        if (line == null) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                                        }
                                        splitline = line.TrimStart().Split(' ');
                                        if (splitline.Length != 4 && splitline.Length != 5) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": signal definition expected");
                                        }
                                        if (!id.IsMatch(splitline[0]) || !Int32.TryParse(splitline[0].Remove(splitline[0].Length - 1), out x) || x != i) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong signal index. \"<id>:\" expected");
                                        }
                                        if (!signal_nature_name.IsMatch(splitline[1])) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong signal nature or name");
                                        }
                                        if (!signal_actionindex.IsMatch(splitline[2])) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong action index");
                                        }
                                        if (!signal_channel.IsMatch(splitline[3])) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong signal channel");
                                        }
                                        if (splitline.Length == 5 && !signal_bool.IsMatch(splitline[4])) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong signal Boolean variable");
                                        }
                                        int nature = -1;
                                        switch (splitline[1].Split(':')[0]) {
                                            case "input":
                                                nature = PredefinedObjects.input;
                                                break;
                                            case "output":
                                                nature = PredefinedObjects.output_s;
                                                break;
                                        }
                                        int channel;
                                        switch (splitline[3].Split(':')[0]) {
                                            case "pure":
                                                channel = PredefinedObjects.pure;
                                                break;
                                            case "single":
                                                channel = PredefinedObjects.single;
                                                break;
                                            case "multiple":
                                                channel = PredefinedObjects.multiple;
                                                break;
                                            default:
                                                channel = -1;
                                                break;
                                        }
                                        program.signals.Add(new Signal(i, nature, splitline[1].Split(':')[1], Int32.TryParse(splitline[2], out x) ? (x) : (-1), channel, Int32.Parse(splitline[3].Split(':')[1]), (splitline.Length == 5) ? (Int32.Parse(splitline[4].Split(':')[1])) : (-1)));
                                    }
                                    //end:
                                    linecounter++;
                                    line = sr.ReadLine();
                                    if (line == null || !(line.Equals("end: ") || line.Equals("end:"))) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"end:\" expected");
                                    }
                                    break;
                                case "implications:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    throw new SyntaxException("Error: table \"implications\" is not supported");
                                case "exclusions:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    throw new SyntaxException("Error: table \"exclusions\" is not supported!");
                                case "variables:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    for (int i = 0; i < count; i++) {
                                        linecounter++;
                                        line = sr.ReadLine();
                                        if (line == null) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                                        }
                                        splitline = line.TrimStart().Split(' ');
                                        if (splitline.Length != 2 && splitline.Length != 4) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": variable definition expected");
                                        }
                                        if (!id.IsMatch(splitline[0]) || !Int32.TryParse(splitline[0].Remove(splitline[0].Length - 1), out x) || x != i) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong variable index. \"<id>:\" expected");
                                        }
                                        if (!type.IsMatch(splitline[1])) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong type index. \"$<type>\" expected");
                                        }
                                        if (splitline.Length == 4)
                                        {
                                            throw new SyntaxException("Error: initial variable allocation is not supported");
                                            //Maybe  TODO
                                        }
                                        program.variables.Add(new Variable(x, Int32.Parse(splitline[1].Last().ToString()), null));
                                    }
                                    //end:
                                    linecounter++;
                                    line = sr.ReadLine();
                                    if (line == null || !(line.Equals("end: ") || line.Equals("end:"))) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"end:\" expected");
                                    }
                                    break;
                                case "actions:":
                                    if (!Int32.TryParse(line.Split(' ')[1], out count)) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"<table>: <count>\" expected");
                                    }
                                    for (int i = 0; i < count; i++) {
                                        linecounter++;
                                        line = sr.ReadLine();
                                        if (line == null) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                                        }
                                        splitline = line.TrimStart().Split(' ');
                                        if (!id.IsMatch(splitline[0]) || !Int32.TryParse(splitline[0].Remove(splitline[0].Length - 1), out x) || x != i) {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": wrong action index. \"<id>:\" expected");
                                        }
                                        //simple actions
                                        if (action_a.IsMatch(splitline[1])) {
                                            if (splitline.Length != 3 || !Int32.TryParse(splitline[2], out x)) {
                                                throw new SyntaxException("Syntax error in line " + linecounter + ": wrong action syntax. \"<action>: <index>\" expected");
                                            }
                                            int action = -1;
                                            switch (splitline[1].Remove(splitline[1].Length - 1)) {
                                                case "present":
                                                    action = PredefinedObjects.present;
                                                    break;
                                                case "dsz":
                                                    action = PredefinedObjects.dsz;
                                                    break;
                                                case "output":
                                                    action = PredefinedObjects.output_a;
                                                    break;
                                                case "reset":
                                                    throw new Exception("Error in line " + linecounter + ": action \"reset\" is not supported");
                                                case "act":
                                                    throw new Exception("Error in line " + linecounter + ": action \"act\" is not supported");
                                                case "goto":
                                                    throw new Exception("Error in line " + linecounter + ": action \"goto\" is not supported");
                                            }
                                            program.actions.Add(new Action(i, action, x));
                                        }
                                        //if action
                                        else if (action_b.IsMatch(splitline[1])) {
                                            if (splitline.Length != 3) {
                                                throw new SyntaxException("Syntax error in line " + linecounter + ": wrong action syntax. \"<action>: <expression>\" expected");
                                            }
                                            program.actions.Add(new Action(i, PredefinedObjects.if_, splitline[2]));
                                        }
                                        //call and combine action
                                        else if (action_c.IsMatch(splitline[1])) {
                                            if (splitline.Length != 4) {
                                                throw new SyntaxException("Syntax error in line " + linecounter + ": wrong action syntax. \"<action>:$<procedure index> (<variable index>) <expression>\" expected");
                                            }
                                            if (!action_variable_index.IsMatch(splitline[2])) {
                                                throw new SyntaxException("Syntax error in line " + linecounter + ": wrong action syntax. \"<action>:$<procedure index> (<variable index>) <expression>\" expected");
                                            }
                                            int action = -1;
                                            switch (splitline[1].Split(':')[0]) {
                                                case "call":
                                                    action = PredefinedObjects.call;
                                                    break;
                                                case "combine":
                                                    throw new Exception("Error in line " + linecounter + ": action \"combine\" is not supported");
                                            }
                                            program.actions.Add(new Action(i, action, Int32.Parse(splitline[2].Replace("(", "").Replace(")", "")), splitline[3], Int32.Parse(splitline[1].Substring(splitline[1].Length - 1))));
                                        }
                                        else {
                                            throw new SyntaxException("Syntax error in line " + linecounter + ": unknown action");
                                        }
                                    }
                                    //end:
                                    linecounter++;
                                    line = sr.ReadLine();
                                    if (line == null || !(line.Equals("end: ") || line.Equals("end:"))) {
                                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"end:\" expected");
                                    }
                                    break;
                                case "states:":
                                    tables = false;
                                    break;
                                default:
                                    throw new SyntaxException("Syntax error in line " + linecounter + ": table description expected");

                            }
                        }
                    }
                    //states:
                    splitline = line.Split(' ');
                    if (splitline.Length != 2 || !Int32.TryParse(splitline[1], out count)) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": wrong state count. \"state: <count>\" expected");
                    }
                    //startpoint:
                    linecounter++;
                    line = sr.ReadLine();
                    if (line == null) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                    }
                    splitline = line.Split(' ');
                    if (!splitline[0].Equals("startpoint:")) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": startpoint expected");
                    }
                    int startpoint;
                    if (splitline.Length != 2 || !Int32.TryParse(splitline[1], out startpoint) || count <= startpoint) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": wrong startpoint. \"startpoint: <nodeId>\" expected");
                    }
                    //calls:
                    linecounter++;
                    line = sr.ReadLine();
                    if (line == null) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                    }
                    splitline = line.Split(' ');
                    if (!splitline[0].Equals("calls:")) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": call count expected");
                    }
                    if (splitline.Length != 2 || !Int32.TryParse(splitline[1], out x)) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": wrong call count. \"calls: <count>\" expected");
                    }
                    //states
                    for (int i = 0; i < count; i++) {
                        linecounter++;
                        line = sr.ReadLine();
                        if (line == null) {
                            throw new SyntaxException("Syntax error in line " + linecounter + ": Unexpected end of file");
                        }
                        splitline = line.Split(' ');
                        if (!id.IsMatch(splitline[0]) || !Int32.TryParse(splitline[0].Remove(splitline[0].Length - 1), out x) || i != x) {
                            throw new SyntaxException("Syntax error in line " + linecounter + ": specification of node " + i + " expected");
                        }
                        string newline;
                        while (true) {
                            linecounter++;
                            newline = sr.ReadLine();
                            if (newline == null) {
                                throw new SyntaxException("Syntax error in line " + linecounter + ": unexpected end of file");
                            }
                            if (newline.Equals("")) {
                                break;
                            }
                            line = line + newline;
                        }
                        checkDagSpec(line, i);
                        splitline = line.Split(' ');
                        Node root = null;
                        string dag = "";
                        for (int j = 1; j < splitline.Length; j++) {
                            if (splitline[j].Equals("")) {
                                continue;
                            }

                            if (state_number.IsMatch(splitline[j])) {
                                dag = dag + splitline[j];
                                if (j + 1 < splitline.Length && state_number_bracket.IsMatch(splitline[j + 1])) {
                                    dag = dag + "|";
                                }
                            }
                            else {
                                dag = dag + splitline[j];
                            }
                        }
                        List<Node> nodes = new List<Node>();
                        root = parseDagSpec(dag, i, nodes);
                        program.states.Add(new State(i, new Dag(root)));
                    }
                    //end:
                    linecounter++;
                    line = sr.ReadLine();
                    if (line == null || !(line.Equals("end: ") || line.Equals("end:"))) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"end:\" expected");
                    }
                    //newline
                    linecounter++;
                    line = sr.ReadLine();
                    if (line == null || !line.Equals("")) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": newline expected");
                    }
                    //endmodule:
                    linecounter++;
                    line = sr.ReadLine();
                    if (line == null || !(line.Equals("endmodule: ") || line.Equals("endmodule:"))) {
                        throw new SyntaxException("Syntax error in line " + linecounter + ": \"endmodule:\" expected");
                    }
                    referenceObjects();
                    program.state = startpoint;
                }
            }
            catch (Exception e) {
                Console.WriteLine("Error reading the file:");
                Console.WriteLine(e.Message);
                Console.WriteLine("\nPress any key to end...");
                Console.ReadKey();
            }
        }

        private void checkDagSpec(string dag, int state) {
            int openBracket = 0;
            int closeBracket = 0;
            int countBracket = 0;
            for (int i = dag.Split(' ')[0].Length; i < dag.Length; i++) {
                if (dag.ElementAt(i) == '(') {
                    openBracket++;
                    countBracket++;
                }
                else if (dag.ElementAt(i) == ')') {
                    closeBracket++;
                    countBracket--;
                }
                else if (dag.ElementAt(i) == ' ' || dag.ElementAt(i) == '0' || dag.ElementAt(i) == '1' || dag.ElementAt(i) == '2' || dag.ElementAt(i) == '3' || dag.ElementAt(i) == '4' || dag.ElementAt(i) == '5'
                     || dag.ElementAt(i) == '6' || dag.ElementAt(i) == '7' || dag.ElementAt(i) == '8' || dag.ElementAt(i) == '9' || dag.ElementAt(i) == '<' || dag.ElementAt(i) == '>') {
                    continue;
                }
                else {
                    throw new SyntaxException("Syntax error: specification of state " + state + " contains unexpected character \'" + dag.ElementAt(i) + "\'");
                }
                if (countBracket < 0) {
                    throw new SyntaxException("Syntax error: specification of state " + state + " contains bracket syntax error");
                }
            }
            if (openBracket != closeBracket) {
                throw new SyntaxException("Syntax error: specification of state " + state + " contains bracket syntax error");
            }
        }

        private void referenceObjects() {
            for (int i = 0; i < program.signals.Count; i++) {
                if (program.signals[i].getVariableId() >= program.variables.Count) {
                    throw new SyntaxException("Syntax error: the variable " + program.signals[i].getVariableId() + " referenced at signal " + i + " does not exist");
                }
                program.signals[i].setVariable(program.variables[program.signals[i].getVariableId()]);
            }
        }

        private Action getAction(int node, int index) {
            if (program.actions.Count <= index) {
                throw new SyntaxException("Syntax error: the action " + index + " referenced in nodedescription of node " + node + " doesn´t exist");
            }
            return program.actions[index];
        }

        private Node parseDagSpec(string dag, int i, List<Node> cont) {
            Node root = null;
            Node last = null;
            while (true) {
                if (dag.Equals("")) {
                    break;
                }
                if (dag.StartsWith("|")) {
                    dag = dag.Substring(1);
                    continue;
                }
                if (dag.StartsWith("(")) {
                    if (last == null) {
                        throw new SyntaxException("Syntax error: syntax error in dag specification of state " + i);
                    }
                    int left = 0;
                    int right = 0;
                    int bracket = 0;
                    for (int a = 1; a < dag.Length; a++) {
                        if (dag.ElementAt(a) == ')' && bracket == 0) {
                            left = a - 1;
                            for (int b = a + 2; b < dag.Length; b++) {
                                if (dag.ElementAt(b) == ')' && bracket == 0) {
                                    right = b - 1;
                                    break;
                                }
                                else if (dag.ElementAt(b) == ')' && bracket > 0) {
                                    bracket--;
                                }
                                else if (dag.ElementAt(b) == '(') {
                                    bracket++;
                                }
                            }
                            break;
                        }
                        else if (dag.ElementAt(a) == ')' && bracket > 0) {
                            bracket--;
                        }
                        else if (dag.ElementAt(a) == '(') {
                            bracket++;
                        }
                    }
                    if (left == 0 || right == 0) {
                        throw new SyntaxException("Syntax error: syntax error in dag specification of state " + i);
                    }
                    List<Node> cont_left = new List<Node>();
                    List<Node> cont_right = new List<Node>();
                    last.setLeft(parseDagSpec(dag.Substring(1, left), i, cont_left));
                    last.setRight(parseDagSpec(dag.Substring(left + 3, right - left - 2), i, cont_right));
                    cont.AddRange(cont_left);
                    cont.AddRange(cont_right);
                    if (last.getLeft() == null || last.getRight() == null) {
                        cont.Add(last);
                    }
                    dag = dag.Substring(right + 2);
                    continue;

                }
                int index = (Math.Min(dag.IndexOf("|"), dag.IndexOf("(")) == -1)
                    ? (Math.Max(dag.IndexOf("|"), dag.IndexOf("(")))
                    : (Math.Min(dag.IndexOf("|"), dag.IndexOf("(")));
                index = (Math.Min(dag.IndexOf("<"), index) == -1)
                    ? (Math.Max(dag.IndexOf("<"), index))
                    : (Math.Min(dag.IndexOf("<"), index));
                if (index == -1) {
                    index = dag.Length;
                }
                int x;
                if (Int32.TryParse(dag.Substring(0, index), out x)) {
                    if (cont.Count > 0) {
                        Node temp = new Node(getAction(i, x), program);
                        foreach (Node n in cont) {
                            if (n.getAction().getAction() == 1) {
                                if (n.getLeft() == null) {
                                    n.setLeft(temp);
                                }
                                if (n.getRight() == null) {
                                    n.setRight(temp);
                                }
                            }
                            else {
                                n.setLeft(temp);
                            }
                        }
                        cont.Clear();
                        last = temp;
                        dag = dag.Substring(index);
                    }
                    else {
                        if (root == null) {
                            root = new Node(getAction(i, x), program);
                            last = root;
                            if (Int32.TryParse(dag, out x)) {
                                cont.Add(last);
                            }
                            dag = dag.Substring(index);
                        }
                        else {
                            last.setLeft(new Node(getAction(i, x), program));
                            last = last.getLeft();
                            if (Int32.TryParse(dag, out x)) {
                                cont.Add(last);
                            }
                            dag = dag.Substring(index);
                        }
                    }
                }
                else if (state_jump.IsMatch(dag)) {
                    index = (Math.Min(dag.IndexOf("|"), dag.IndexOf("(")) == -1)
                        ? (Math.Max(dag.IndexOf("|"), dag.IndexOf("(")))
                        : (Math.Min(dag.IndexOf("|"), dag.IndexOf("(")));
                    if (index == -1) {
                        index = dag.Length;
                    }
                    if (cont.Count > 0) {
                        Node temp;
                        try {
                            temp = new Node(Int32.Parse(dag.Substring(0, index).TrimStart('<').TrimEnd('>')), program);
                        }
                        catch (Exception) {
                            throw new SyntaxException("Syntax error: syntax error in dag specification of state " + i);
                        }
                        foreach (Node n in cont) {
                            if (n.getAction().getAction() == 1) {
                                if (n.getLeft() == null) {
                                    n.setLeft(temp);
                                }
                                if (n.getRight() == null) {
                                    n.setRight(temp);
                                }
                            }
                            else {
                                n.setLeft(temp);
                            }
                        }
                        cont.Clear();
                        last = temp;
                        dag = dag.Substring(index);
                    }
                    else {
                        if (root == null) {
                            root = new Node(Int32.Parse(dag.Substring(0, index).TrimStart('<').TrimEnd('>')), program);
                            last = root;
                            dag = dag.Substring(index);
                        }
                        else {
                            last.setLeft(new Node(Int32.Parse(dag.Substring(0, index).TrimStart('<').TrimEnd('>')), program));
                            last = last.getLeft();
                            dag = dag.Substring(index);
                        }
                    }
                }
                else {
                    throw new SyntaxException("Syntax error: syntax error in dag specification of state " + i);
                }

            }
            return root;
        }
    }
}
