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

namespace SafetyLustre
{
    public class Node
    {

        private Node left;
        private Node right;
        private Action action;
        private int state;
        private Program program;

        public Node(Action action, Program program)
        {
            this.action = action;
            state = -1;
            this.program = program;
        }

        public Node(int state, Program program)
        {
            this.state = state;
            this.program = program;
        }

        public bool setLeft(Node left)
        {
            if (this.left != null)
            {
                return false;
            }
            this.left = left;
            return true;
        }

        public bool setRight(Node right)
        {
            if (this.right != null)
            {
                return false;
            }
            this.right = right;
            return true;
        }

        public Node getLeft()
        {
            return left;
        }

        public Node getRight()
        {
            return right;
        }

        public Action getAction()
        {
            return action;
        }

        public int getState()
        {
            return state;
        }

        public bool executeAction()
        {
            switch (action.getAction())
            {
                case 0:
                    return true;
                case 1:
                    return (bool)program.executeExpression(getAction().getExpression());
                case 2:
                    if (getAction().getIndex() < program.variables.Count)
                    {
                        if (program.variables[getAction().getIndex()].getType() == 1)
                        {
                            program.variables[getAction().getIndex()]
                                .setValue((int)program.variables[getAction().getIndex()].getValue() - 1);
                            return ((int)program.variables[getAction().getIndex()].getValue() <= 0);
                        }
                        else
                        {
                            throw new SyntaxException("The variable to be decremented is not of type integer");
                        }
                    }
                    else
                    {
                        throw new SyntaxException("The variable to be decremented can not be referenced");
                    }
                // Note: no break!
                case 3:
                    if (getAction().getIndex() < program.signals.Count)
                    {
                        if (!Program.modelChecking)
                        {
                            Program.outputTextWriter.WriteLine("Signal " + program.signals[getAction().getIndex()].getName() + " returns: " + program.signals[getAction().getIndex()].getVariable().getValue().ToString());
                        }
                        program.output.Add(program.signals[getAction().getIndex()].getVariable().getValue());
                    }
                    else
                    {
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
                    if (getAction().getIndex() < program.variables.Count)
                    {
                        if (getAction().getAllocationAction() == program.variables[getAction().getIndex()].getType())
                        {
                            program.variables[getAction().getIndex()].setValue(program.executeExpression(getAction().getExpression()));
                        }
                        else
                        {
                            throw new SyntaxException("Variable of type $" + program.variables[getAction().getIndex()].getType() + " can not be assigned value of type $" + getAction().getAllocationAction());
                        }
                    }
                    else
                    {
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
