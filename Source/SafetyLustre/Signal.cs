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
    public class Signal {

        private int id;
        private int nature;
        private string name;
        private int actionindex;
        private int channel;
        private int variableId;
        private Variable variable;
        private int boolean;

	    public bool IsRealInput { get; private set; }
		public bool IsFaultInput { get; private set; }

		public Signal(int id, int nature, string name, int actionindex, int channel, int variableId, int boolean) {
            this.id = id;
            this.nature = nature;
            this.name = name;
            this.channel = channel;
            this.actionindex = actionindex;
            this.variableId = variableId;
            this.boolean = boolean;

			if (this.nature == PredefinedObjects.input && name.StartsWith("fault"))
			{
				IsFaultInput = true;
			}
			if (this.nature == PredefinedObjects.input && !name.StartsWith("fault"))
			{
				IsRealInput = true;
			}
		}

        public void setVariable(Variable variable) {
            this.variable = variable;
        }

        public int getNature() {
            return this.nature;
        }

        public int getVariableId() {
            return this.variableId;
        }

        public Variable getVariable() {
            return this.variable;
        }

        public string getName() {
            return this.name;
        }

    }
}
