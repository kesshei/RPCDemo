using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPCDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "RPC从零实现RPC Demo by 蓝创精英团队!";
            var demo = DnamicInterfaceProxy.Resolve<IDemo>();
            DnamicInterfaceProxy.Save();
            demo.Say();
            demo.Say("123");
            demo.Say(5, new List<string>() { "1", "2", "3" }, Kind.a);
            demo.Say("demo", 6, new List<string>() { "6" }, Kind.b);
            demo.Test = new List<string>() { "11", "12" };
            var b = demo.Test;
            Console.ReadLine();
        }
    }
    public interface IDemo
    {
        void Say();
        string Say(string msg);
        int Say(string a, int b, List<string> c, Kind kind);
        string Say(int b, List<string> c, Kind kind);

        List<string> Test { get; set; }
    }
    public enum Kind
    {
        a,
        b
    }
}
