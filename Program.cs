using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public class Process
{
    public string Id { get; }
    private int privateLabel;
    private int publicLabel;
    private readonly object lockObj = new();

    public List<Process> WaitingFor { get; } = new();
    public static ConcurrentDictionary<string, Process> AllProcesses = new();

    public Process(string id)
    {
        Id = id;
        privateLabel = GenerateLabel();
        publicLabel = privateLabel;
        AllProcesses[id] = this;

        Console.WriteLine($"🔵 Process {Id} initialized with label {privateLabel}");
    }

    private static int labelCounter = 0;
    private static int GenerateLabel() => ++labelCounter;

    public void OnBlock()
    {
        privateLabel = labelCounter;
        publicLabel = labelCounter;
    }
    public void WaitFor(params string[] processIds)
    {
        lock (lockObj)
        {
            Console.WriteLine($"\n {Id} is now waiting for: {string.Join(", ", processIds)}");

            foreach (var pid in processIds)
            {
                if (AllProcesses.TryGetValue(pid, out var proc))
                {
                    WaitingFor.Add(proc);
                }
            }
            GenerateLabel();
            OnBlock();
            Console.WriteLine($" {Id} updated public label to {publicLabel}");

            PropagateLabel(publicLabel);
        }
    }

    private void PropagateLabel(int label)
    {
        foreach (var proc in WaitingFor)
        {
            lock (proc.lockObj)
            {
                Console.WriteLine($" {Id} propagating label {label} to {proc.Id} (current: {proc.publicLabel})");

                if (label > proc.publicLabel)
                {
                    proc.publicLabel = label;
                    Console.WriteLine($" {proc.Id} accepted label {label} (new public label)");

                    proc.PropagateLabel(label);
                }
//                else if (proc == origin && label == proc.publicLabel)
//              We actually do know the origin process so we can compare just that but to
//              follow the assignement description better I'm comparing the labels 
//              
                else if (proc.publicLabel ==  proc.privateLabel && label == proc.publicLabel)
                {
                    Console.WriteLine($" DEADLOCK DETECTED by {proc.Id} (label {label} came full circle)");
                }
                else
                {
                    Console.WriteLine($" {proc.Id} ignored label {label} (not greater)");
                }
            }
        }
    }

    public void Release()
    {
        lock (lockObj)
        {
            Console.WriteLine($"\n {Id} released resources.");
            WaitingFor.Clear();
            privateLabel = GenerateLabel();
            publicLabel = privateLabel;
            Console.WriteLine($" {Id} reset to label {privateLabel}");
        }
    }

    public override string ToString() => $"{Id} [Public: {publicLabel}]";
}

public class Program
{
    public static void Main()
    {
        // Note: Once we reach a deadlock every following call of "waitFor" will result in it finding a deadlock,
        // since we're alredy deadlocked and I'm not implementing the release of a deadlock as it is not a part
        // of the assigment and does not seem trivial. 
        TestScenario1();
        //TestScenario2();


        Console.WriteLine("\n===== Simulation Complete =====");
    }

public static void TestScenario1()
{
    var a = new Process("A");
    var b = new Process("B");
    var c = new Process("C");
    var d = new Process("D");
    var e = new Process("E");
    var f = new Process("F");

    a.WaitFor("B", "C");
    b.WaitFor("D");
    c.WaitFor("E");
    d.WaitFor("F");
    e.WaitFor("A"); // Deadlock
    f.WaitFor("C");
}

public static void TestScenario2()
    {
        var p1 = new Process("P1");
        var p2 = new Process("P2");
        var p3 = new Process("P3");
        var p4 = new Process("P4");
        var p5 = new Process("P5");
        var p6 = new Process("P6");

        p1.WaitFor("P2", "P3");
        p2.WaitFor("P4");
        p3.WaitFor("P5");
        p4.WaitFor("P6");
        p5.WaitFor("P1");  // Deadlock
        p6.WaitFor("P3");  
    }
}
