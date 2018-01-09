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
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BachelorarbeitLustre {
    static class PredefinedObjects {

        //actions
        public const int present = 0;
        public const int if_ = 1;
        public const int dsz = 2;
        public const int output_a = 3;
        public const int reset = 4;
        public const int act = 5;
        public const int goto_ = 6;
        public const int call = 7;
        public const int combine = 8;

        //nature of signals
        public const int input = 0;
        public const int output_s = 1;
        public const int ret = 2;
        public const int local = 3;

        //channel of signal
        public const int pure = 0;
        public const int single = 1;
        public const int multiple = 2;

        public static Object executeFunction(int id, Object x, Object y, Object z) {
            try
            {
                switch (id)
                {
                    case 0:
                        if (x.Equals(y))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 1:
                        if (!x.Equals(y))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 2:
                        if (x.Equals(true))
                        {
                            return (bool) y;
                        }
                        else
                        {
                            return (bool) z;
                        }
                    case 3:
                        if (x.Equals(true) || y.Equals(true))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 4:
                        if (x.Equals(true) && y.Equals(true))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 5:
                        if (x.Equals(true))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    case 6:
                        if ((int) x == (int) y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 7:
                        if ((int) x != (int) y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 8:
                        if (x.Equals(true))
                        {
                            return (int) y;
                        }
                        else
                        {
                            return (int) z;
                        }
                    case 9:
                        if ((int) x < (int) y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 10:
                        if ((int) x <= (int) y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 11:
                        if ((int) x > (int) y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 12:
                        if ((int) x >= (int) y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 13:
                        return (int) x + (int) y;
                    case 14:
                        return (int) x - (int) y;
                    case 15:
                        return (int) x * (int) y;
                    case 16:
                        return (int) x % (int) y;
                    case 17:
                        return (int) ((int) x / (int) y);
                    case 18:
                        return -(int) x;
                    case 19:
                        return (int) x | (int) y;
                    case 20:
                        return ~(int) x & ~(int) y;
                    case 21:
                        return ~(int) x;
                    case 22:
                        if (x.ToString().Equals(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 23:
                        if (!x.ToString().Equals(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 24:
                        if (x.Equals(true))
                        {
                            return y.ToString();
                        }
                        else
                        {
                            return z.ToString();
                        }
                    case 25:
                        if (float.Parse(x.ToString()) == float.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 26:
                        if (float.Parse(x.ToString()) != float.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 27:
                        if (x.Equals(true))
                        {
                            return float.Parse(y.ToString());
                        }
                        else
                        {
                            return float.Parse(z.ToString());
                        }
                    case 28:
                        if (float.Parse(x.ToString()) < float.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 29:
                        if (float.Parse(x.ToString()) <= float.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 30:
                        if (float.Parse(x.ToString()) > float.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 31:
                        if (float.Parse(x.ToString()) >= float.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 32:
                        return float.Parse(x.ToString()) + float.Parse(y.ToString());
                    case 33:
                        return float.Parse(x.ToString()) - float.Parse(y.ToString());
                    case 34:
                        return float.Parse(x.ToString()) * float.Parse(y.ToString());
                    case 35:
                        return (float.Parse(x.ToString()) / float.Parse(y.ToString()));
                    case 36:
                        return -(float.Parse(x.ToString()));
                    case 37:
                        return float.Parse(x.ToString());
                    case 38:
                        if (double.Parse(x.ToString()) == double.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 39:
                        if (double.Parse(x.ToString()) != double.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 40:
                        if (x.Equals(true))
                        {
                            return double.Parse(y.ToString());
                        }
                        else
                        {
                            return double.Parse(z.ToString());
                        }
                    case 41:
                        if (double.Parse(x.ToString()) < double.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 42:
                        if (double.Parse(x.ToString()) <= double.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 43:
                        if (double.Parse(x.ToString()) > double.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 44:
                        if (double.Parse(x.ToString()) >= double.Parse(y.ToString()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    case 45:
                        return double.Parse(x.ToString()) + double.Parse(y.ToString());
                    case 46:
                        return double.Parse(x.ToString()) - double.Parse(y.ToString());
                    case 47:
                        return double.Parse(x.ToString()) * double.Parse(y.ToString());
                    case 48:
                        return (double.Parse(x.ToString()) / double.Parse(y.ToString()));
                    case 49:
                        return -(double.Parse(x.ToString()));
                    default:
                        return true;
                }
            }
            catch (Exception)
            {
                throw new SyntaxException("One of the parameters could not parsed to expected data type");
            }
        }
    }
}
