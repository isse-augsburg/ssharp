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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre
{
    public class Constant {

        private int id;
        private string name;
        private int type;
        private bool b;
        private int i;
        private string s;
        private float f;
        private double d;

        public Constant(int id, string name, int type, string value) {
            this.id = id;
            this.name = name;
            this.type = type;
            switch (type) {
                case 0:
                    b = Boolean.Parse(value);
                    break;
                case 1:
                    i = Int32.Parse(value);
                    break;
                case 2:
                    s = value;
                    break;
                case 3:
                    f = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case 4:
                    d = double.Parse(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        public Constant(int id, string name, int type) {
            this.id = id;
            this.name = name;
            this.type = type;
        }

        public Object getValue() {
            switch (type) {
                case 0:
                    return b;
                case 1:
                    return i;
                case 2:
                    return s;
                case 3:
                    return f;
                case 4:
                    return d;
                default:
                    return b;
            }
        }

    }
}
