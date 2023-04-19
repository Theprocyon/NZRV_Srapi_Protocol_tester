using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ConsoleApp1
{
    internal class Program
    {
        static bool _continue;
        static ConcurrentQueue<Object> _queue;
        static ConcurrentQueue<byte> _readqueue;
        static SerialPort _serialPort;
        static Stopwatch _stopwatch;
        static Parser _parser;
        public static void Main()
        {
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);
            Thread writerThread = new Thread(Write);
            Thread parserThread = new Thread(parseByte);

            _stopwatch = new Stopwatch();
            _queue = new ConcurrentQueue<Object>();
            _readqueue = new ConcurrentQueue<byte>();

            _serialPort = new SerialPort();
            _parser = new Parser();
            _parser.Initialize();

            _serialPort.PortName = SetPortName(_serialPort.PortName);
            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            Console.Clear();

            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            try
            {
                _serialPort.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            _continue = true;

            readThread.Start();
            writerThread.Start();
            parserThread.Start();

            readThread.Join();
            writerThread.Join();
            parserThread.Join();

            _serialPort.Close();
        }

        public static void parseByte()
        {
            while (_continue)
            {
                if (!_readqueue.IsEmpty)
                {
                    byte _c;
                    
                    while (_readqueue.TryDequeue(out _c) == false) { Thread.Sleep(10); }

                    if (_parser.parseChar(_c) != "")
                    {
                        if (_stopwatch.IsRunning)
                        {
                            _stopwatch.Stop();
                            Console.WriteLine("Interval Time is : " + _stopwatch.ElapsedMilliseconds);
                            _stopwatch.Restart();
                        }
                        else
                        {
                            _stopwatch.Start();
                        }
                        _parser.Initialize();
                        _queue.Enqueue(new object());
                    }
                }
            }
        }

        public static void Read()
        {
            while (_continue)
            {
                try
                {
                    int num = _serialPort.BytesToRead;
                    byte[] rb = new byte[num];
                    _serialPort.Read(rb, 0, num);

                    for (int i = 0; i < num; i++)
                    {
                        _readqueue.Enqueue(rb[i]);
                    }
                }
                catch (TimeoutException) { }
            }
        }
        public static void Write()
        {
            while (_continue)
            {
                try
                {
                    if (!_queue.IsEmpty)
                    {
                        Object obj;
                        while (_queue.TryDequeue(out obj) == false) { };

                        Console.SetCursorPosition(Console.CursorTop, Console.CursorLeft);

                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write((byte)0x02);
                        writer.Write(Encoding.ASCII.GetBytes("/api/module/v1/pmc/bat/status"));
                        writer.Write((byte)0x1D);
                        writer.Write((byte)0x00);
                        writer.Write((byte)0xC8);
                        writer.Write((byte)0x1F);
                        writer.Write(Encoding.ASCII.GetBytes("{\r\n\"type\" : \"pmc\"\r\n}\r\n"));
                        writer.Write((byte)0x1F);
                        writer.Write((byte)0xB0); // Chksum
                        writer.Write((byte)0x03);
                        byte[] bf = stream.ToArray();

                        _serialPort.Write(bf, 0, bf.Length);

                    }

                }
                catch (Exception e)
                {
                    //Swallow
                }
            }
        }

        public static string SetPortName(string defaultPortName)
        {
            string portName;

            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("COM port({0}): ", defaultPortName);
            portName = Console.ReadLine();

            if (portName == "")
            {
                portName = defaultPortName;
            }
            return portName;
        }
    }
}