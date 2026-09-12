using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Auga
{
    public static partial class API
    {
        public static string EchoString(string value) => Invoke<string>("Echo", new[] { typeof(string) }, new object[] { value });
        public static string EchoObject(object value) => Invoke<string>("Echo", new[] { typeof(object) }, new object[] { value });
        public static int Wires(RequirementWireState[] values) => Invoke<int>("Wires", new[] { typeof(RequirementWireState[]) }, new object[] { values });
        public static PlayerPanelTabData Tab(object value) => Invoke<PlayerPanelTabData>("Tab", new[] { typeof(object) }, new[] { value });
        public static int Callback(Func<int, int> callback) => Invoke<int>("Callback", new[] { typeof(Func<int, int>) }, new object[] { callback });
        public static bool IsReady() => Invoke<bool>("IsReady", Type.EmptyTypes, Array.Empty<object>());
        public static void Missing() => Invoke<object>("Missing", Type.EmptyTypes, Array.Empty<object>());
        public static void Fail() => Invoke<object>("Fail", Type.EmptyTypes, Array.Empty<object>());
    }
}

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        _checks++;
    }
    public static void Main(string[] args)
    {
        Check(!Auga.API.IsLoaded(), "absent plugin");
        Check(Auga.API.EchoString("first") == null, "absent call has default return");
        AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
        Check(Auga.API.IsLoaded(), "late load after first call");
        Check(Auga.API.EchoString("x") == "string:x", "string overload");
        Check(Auga.API.EchoObject("x") == "object:x", "object overload");
        Check(Auga.API.EchoObject(null) == "object:", "typed null overload");
        Check(Auga.API.Wires(new[] { Auga.RequirementWireState.Have, Auga.RequirementWireState.DontHave }) == 3, "enum array conversion");
        Check(Auga.API.Wires(null) == -1, "null array");
        object reference = new object();
        var tab = Auga.API.Tab(reference);
        Check(tab.Index == 7 && tab.Title == "mod tab" && ReferenceEquals(tab.Reference, reference), "DTO conversion and reference identity");
        Check(Auga.API.Callback(value => value * 3) == 12, "delegate forwarding");
        Check(!Auga.API.IsReady(), "old runtime capability probe");
        try { Auga.API.Missing(); throw new Exception("Expected missing method error"); }
        catch (NotSupportedException) { _checks++; }
        try { Auga.API.Fail(); throw new Exception("Expected runtime error"); }
        catch (InvalidOperationException exception) { Check(exception.Message == "runtime failure", "unwrap runtime exception"); }
        Console.WriteLine($"{_checks} API bridge checks passed.");
    }
}
