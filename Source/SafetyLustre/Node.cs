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
using System.Threading.Tasks;

namespace BachelorarbeitLustre {
    public class Node {

        private Node left;
        private Node right;
        private Action action;
        private int state;
        private Program program;

        public Node(Action action, Program program) {
            this.action = action;
            this.state = -1;
            this.program = program;
        }

        public Node(int state, Program program) {
            this.state = state;
            this.program = program;
        }

        public bool setLeft(Node left) {
            if (this.left != null) {
                return false;
            }
            this.left = left;
            return true;
        }

        public bool setRight(Node right) {
            if (this.right != null) {
                return false;
            }
            this.right = right;
            return true;
        }

        public Node getLeft() {
            return this.left;
        }

        public Node getRight() {
            return this.right;
        }

        public Action getAction() {
            return this.action;
        }

        public int getState() {
            return this.state;
        }

        public bool executeAction() {
            switch (action.getAction()) {
                case 0:
                    return true;
                case 1:
                    return (bool)program.executeExpression(this.getAction().getExpression());
                case 2:
                    if (this.getAction().getIndex() < program.variables.Count) {
                        if (program.variables[this.getAction().getIndex()].getType() == 1) {
                            program.variables[this.getAction().getIndex()]
                                .setValue((int)program.variables[this.getAction().getIndex()].getValue() - 1);
                            return ((int)program.variables[this.getAction().getIndex()].getValue() <= 0);
                        }
                        else {
                            throw new SyntaxException("The variable to be decremented is not of type integer");
                        }
                    }
                    else {
                        throw new SyntaxException("The variable to be decremented can not be referenced");
                    }
                case 3:
                    if (this.getAction().getIndex() < program.signals.Count) {
                        if (!Program.modelChecking)
                        {
							Program.outputTextWriter.WriteLine("Signal " + program.signals[this.getAction().getIndex()].getName() + " returns: " + program.signals[this.getAction().getIndex()].getVariable().getValue().ToString());
                        }
                        program.output.Add(program.signals[this.getAction().getIndex()].getVariable().getValue());
                    }
                    else {
                        throw new SyntaxException("The signal to be put out can not be referenced");
                    }
                    return true;
                case 4:
                    return true;
                case 5:
                    return true;
                case 6:
                    return true;
                case 7:
                    if (this.getAction().getIndex() < program.variables.Count) {
                        if (this.getAction().getAllocationAction() == program.variables[this.getAction().getIndex()].getType()) {
                            program.variables[this.getAction().getIndex()].setValue(program.executeExpression(this.getAction().getExpression()));
                        }
                        else {
                            throw new SyntaxException("Variable of type $" + program.variables[this.getAction().getIndex()].getType() + " can not be assigned value of type $" + this.getAction().getAllocationAction());
                        }
                    }
                    else {
                        throw new SyntaxException("The variable to be assigned can not be referenced");
                    }
                    return true;
                case 8:
                    return true;
                default:
                    return true;
            }
        }

    }
}
