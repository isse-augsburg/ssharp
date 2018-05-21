// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2018, Pascal Pfeil
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

using SafetyLustre.Oc5Compiler;
using System;
using System.Diagnostics;
using System.IO;

namespace SafetyLustre
{
    public class Program
    {
        static void Main(string[] args)
        {
            var oc = LustreCompiler.Compile(File.ReadAllText(@"D:\Bibliotheken\Downloads\lustre-v4-III-db-linux64\examples\parity\parity.lus"), "parity");
            Console.WriteLine(oc);
            Console.WriteLine("Programmende!");
            Console.ReadKey(true);

            return;

            var oc5 = File.ReadAllText(@"C:\Users\Pascal\Source\University\ssharp\SafetyLustreTest\Examples\PressureTank.oc");

            var stopwatch = Stopwatch.StartNew();
            var runner = new Oc5Compiler.Oc5Runner(oc5);
            stopwatch.Stop();
            Console.WriteLine($"Compilation finished after {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:00}");


            var ticksCount = 1000000;
            var rand = new Random();
            stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < ticksCount; i++)
            {
                bool randomBool = rand.NextDouble() > 0.5;
                runner.Tick(randomBool);
            }

            stopwatch.Stop();

            Console.WriteLine($"{ticksCount} ticks finished after {stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds:00}");

            Console.ReadKey(true);
        }
    }
}
