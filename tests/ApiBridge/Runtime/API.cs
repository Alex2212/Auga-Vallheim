using System;
namespace Auga
{
    public static class API
    {
        public static string Echo(string value) => "string:" + value;
        public static string Echo(object value) => "object:" + value;
        public static int Wires(RequirementWireState[] values) => values == null ? -1 : (int)values[0] + (int)values[1];
        public static PlayerPanelTabData Tab(object reference) => new PlayerPanelTabData { Index = 7, Title = "mod tab", Reference = reference };
        public static int Callback(Func<int, int> callback) => callback(4);
        public static void Fail() => throw new InvalidOperationException("runtime failure");
    }
}
