﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace TME
{
    class Instruction
    {
        public Instruction()
        {
        }

        public Instruction(int newSymbol, int direction, int newState)
        {
            if (newSymbol != 0 && newSymbol != 1)
                throw new ArgumentOutOfRangeException("newSymbol");
            if (direction != 0 && direction != 1)
                throw new ArgumentOutOfRangeException("direction");
            if (newState < 0)
                throw new ArgumentOutOfRangeException("newState");
            NewSymbol = newSymbol;
            Direction = direction;
            NewState = newState;
        }

        public Instruction(ref BigInteger value, int stateCount)
        {
            BigInteger newState;
            BigInteger direction;
            BigInteger newSymbol;
            value = BigInteger.DivRem(value, stateCount, out newState);
            value = BigInteger.DivRem(value, 2, out direction);
            value = BigInteger.DivRem(value, 2, out newSymbol);
            NewSymbol = (int)newSymbol;
            Direction = (int)direction;
            NewState = (int)newState;
        }

        public void Encode(ref BigInteger result, int stateCount)
        {
            result *= 2;
            result += NewSymbol;
            result *= 2;
            result += Direction;
            result *= stateCount;
            result += NewState;
        }

        public int NewSymbol;
        public int Direction; // 0 - Left, 1 - Right
        public int NewState;  // 0 - Halt
    }

    class Transition
    {
        public Transition()
        {
            Instructions = new Instruction[2]
            {
                new Instruction(),
                new Instruction()
            };
        }

        public Transition(
            int NewSymbol0, int Direction0, int NewState0,
            int NewSymbol1, int Direction1, int NewState1)
        {
            Instructions = new Instruction[2]
            {
                new Instruction (NewSymbol0, Direction0, NewState0),
                new Instruction (NewSymbol1, Direction1, NewState1)
            };
        }

        public Transition(ref BigInteger value, int stateCount)
        {
            Instructions = new Instruction[2];
            for (int i = 0; i < 2; i++)
                Instructions[1 - i] = new Instruction(ref value, stateCount);
        }

        public void Encode(ref BigInteger result, int stateCount)
        {
            foreach (var inst in Instructions)
                inst.Encode(ref result, stateCount);
        }

        public Instruction[] Instructions;
    }

    class Program
    {
        Program()
        {
            // Test
            //var tape = new Dictionary<int, int>();
            //var machine = DecodeBI(22580604002);
            //int steps = Run(machine, tape);
            //if (steps != 107)
            //    throw new Exception();
        }

        void ParseFormat(string ext, out int format, ref int @base)
        {
            if (ext == "bin")
            {
                format = 0;
            }
            else if (ext.StartsWith("b") &&
                int.TryParse(ext.Substring(1), out @base) &&
                @base >= 2 && @base <= 36)
            {
                format = 1;
            }
            else if (ext == "jst")
            {
                format = 2;
            }
            else
                throw new NotSupportedException("Unsupported format");
        }

        void Process(string input, string output)
        {
            // 0 - binary (base 256)
            // 1 - baseN text
            // 2 - jsturing
            int inputFormat = 1;
            int inputBase = 10;
            int outputFormat = 2;
            int outputBase = 10;
            byte[] inputData = null;
            string outputName = output;
            if (input.Contains('.'))
            {
                ParseFormat(input.Split('.')[1], out inputFormat, ref inputBase);
                inputData = File.ReadAllBytes(input);
            }
            else
            {
                inputData = Encoding.UTF8.GetBytes(input);
            }
            if (inputFormat == 2)
                outputFormat = 1;
            if (output != null)
            {
                if (output.Contains('.'))
                    ParseFormat(output.Split('.')[1], out outputFormat, ref outputBase);
                else
                    throw new Exception("Output format is not specified");
            }
            Transition[] machine = Decode(inputData, inputFormat, inputBase);
            byte[] outputData = Encode(machine, outputFormat, outputBase);
            if (output != null)
                File.WriteAllBytes(output, outputData);
            else
                Console.WriteLine(Encoding.UTF8.GetString(outputData));
        }

        BigInteger GetMachineCount(int size)
        {
            BigInteger count = BigInteger.Pow(size + 1, size * 2);
            count <<= 4 * size;
            return count;
        }

        Transition[] Decode(byte[] data, int format, int @base)
        {
            BigInteger bi = BigInteger.Zero;
            if (format == 2)
            {
                var machine = new List<Transition>();
                var machineRaw = new List<string[]>();
                var stateNames = new Dictionary<string, int>();
                var syms = new string[2] { "_", "1" };
                var dirs = new string[2] { "l", "r" };
                var lines = Encoding.UTF8.GetString(data).Split(new char[] { '\r', '\n' });
                foreach (var line in lines)
                {
                    int commentIndex = line.IndexOf(';');
                    var lineClean = line;
                    if (commentIndex != -1)
                        lineClean = line.Substring(0, commentIndex);
                    var words = lineClean.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 0)
                        continue;
                    if (words.Length != 5)
                        throw new Exception("Wrong tuple size");
                    machineRaw.Add(words);
                }
                foreach (var instRaw in machineRaw)
                {
                    string curStateS = instRaw[0];
                    if (!stateNames.ContainsKey(curStateS))
                    {
                        stateNames.Add(curStateS, stateNames.Count);
                        machine.Add(new Transition());
                    }
                }
                foreach (var instRaw in machineRaw)
                {
                    string curStateS = instRaw[0];
                    string curSymbolS = instRaw[1];
                    string newSymbolS = instRaw[2];
                    string directionS = instRaw[3].ToLower();
                    string newStateS = instRaw[4];

                    if (!syms.Contains(curSymbolS) || !syms.Contains(newSymbolS))
                        throw new Exception("Only symbols _ and 1 are supported");
                    if (!dirs.Contains(directionS))
                        throw new Exception("Only directions l and r are supported");

                    int newState = 0; // halt
                    if (!newStateS.StartsWith("halt"))
                    {
                        if (!stateNames.ContainsKey(newStateS))
                            throw new Exception($"Unknown new state {newStateS}");
                        else
                            newState = stateNames[newStateS] + 1;
                    }

                    int direction = Array.IndexOf(dirs, directionS);
                    int curSymbol = Array.IndexOf(syms, curSymbolS);
                    int newSymbol = Array.IndexOf(syms, newSymbolS);

                    var instruction = machine[stateNames[curStateS]].Instructions[curSymbol];
                    instruction.NewSymbol = newSymbol;
                    instruction.Direction = direction;
                    instruction.NewState = newState;
                }
                bi = EncodeBI(machine.ToArray());
            }
            else if (format == 1)
            {
                var dataS = Encoding.UTF8.GetString(data).ToUpper().ToArray();
                foreach (char c in dataS)
                {
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                        continue;
                    int digit;
                    if (c >= '0' && c <= '9')
                        digit = c - '0';
                    else if (c >= 'A' && c <= 'Z')
                        digit = c - 'A' + 10;
                    else
                        throw new Exception($"Unexpected digit {c}");
                    if (digit >= @base)
                        throw new Exception($"Digit {c} is out of range for base {@base}");
                    bi *= @base;
                    bi += digit;
                }
            }
            else if (format == 0)
            {
                bi = new BigInteger(new byte[1] { 0 }.Concat(data).Reverse().ToArray());
            }
            else
                throw new NotSupportedException();
            return DecodeBI(bi);
        }

        byte[] Encode(Transition[] machine, int format, int @base)
        {
            if (format == 2)
            {
                var syms = new char[2] { '_', '1' };
                var dirs = new char[2] { 'l', 'r' };
                var sb = new StringBuilder();
                for (int curState = 0; curState < machine.Length; curState++)
                {
                    var tr = machine[curState];
                    for (int j = 0; j < 2; j++)
                    {
                        var instr = tr.Instructions[j];
                        string newState;
                        if (instr.NewState == 0)
                            newState = "halt";
                        else
                            newState = (instr.NewState - 1).ToString();
                        if (curState != 0 || j != 0)
                            sb.AppendLine();
                        sb.Append($"{curState} {syms[j]} {syms[instr.NewSymbol]} " +
                            $"{dirs[instr.Direction]} {newState}");
                    }
                }
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            else if (format == 1)
            {
                var bi = EncodeBI(machine);
                if (bi.IsZero)
                    return Encoding.UTF8.GetBytes("0");

                var sb = new StringBuilder();
                while (bi > BigInteger.Zero)
                {
                    BigInteger digit;
                    bi = BigInteger.DivRem(bi, @base, out digit);
                    char digitC;
                    if (digit < 10)
                        digitC = (char)('0' + (int)digit);
                    else
                        digitC = (char)('A' + (int)digit - 10);
                    sb.Insert(0, digitC);
                }
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            else if (format == 0)
            {
                var arr = EncodeBI(machine).ToByteArray().Reverse().ToArray();
                if (arr.Length > 1 && arr[0] == 0)
                    return arr.Skip(1).ToArray(); // No leading zeroes are needed
                else
                    return arr;
            }
            else
                throw new NotSupportedException();
        }

        Transition[] DecodeBI(BigInteger encoded)
        {
            if (encoded < BigInteger.Zero)
                throw new ArgumentOutOfRangeException("encoded");

            BigInteger offset = BigInteger.Zero;
            for (int i = 1; ; i++)
            {
                BigInteger newOffset = offset + GetMachineCount(i);
                if (encoded < newOffset)
                {
                    BigInteger index = encoded - offset;
                    var machine = new Transition[i];
                    for (int t = 0; t < i; t++)
                        machine[i - t - 1] = new Transition(ref index, i + 1);
                    return machine;
                }
                else
                    offset = newOffset;
            }
        }

        BigInteger EncodeBI(Transition[] machine)
        {
            if (machine.Length == 0)
                throw new NotSupportedException();
            BigInteger offset = BigInteger.Zero;
            for (int i = 1; i < machine.Length; i++)
                offset += GetMachineCount(i);
            BigInteger index = BigInteger.Zero;
            foreach (var tr in machine)
                tr.Encode(ref index, machine.Length + 1);
            return offset + index;
        }

        int Run(Transition[] machine, Dictionary<int, int> tape)
        {
            int steps = 0;
            int state = 1;
            int position = 0;
            for (; state != 0;)
            {
                if (!tape.ContainsKey(position))
                    tape.Add(position, 0);
                var inst = machine[state - 1].Instructions[tape[position]];
                tape[position] = inst.NewSymbol;
                if (inst.Direction == 1)
                    position++;
                else
                    position--;
                state = inst.NewState;
                steps++;
            }
            return steps;
        }

        static void Main(string[] args)
        {
            if (args.Length == 1)
                new Program().Process(args[0], null);
            else if (args.Length == 2)
                new Program().Process(args[0], args[1]);
            else
            {
                Console.WriteLine("Binary Turing machine encoder");
                Console.WriteLine("Usage:");
                Console.WriteLine("  TME input [output]");
                Console.WriteLine("Examples:");
                Console.WriteLine("  TME 48");
                Console.WriteLine("  TME 20317 bb2.b10");
                Console.WriteLine("  TME bb2.b10 bb2.jst");
                return;
            }
        }
    }
}
