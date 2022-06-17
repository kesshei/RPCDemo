using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RPCDemo
{
    /// <summary>
    /// 内置代理对象
    /// </summary>
    public interface InvocationHandler
    {
        object InvokeMember(object obj, int rid, string name, params object[] args);
    }
    public class DefaultInvocationHandler<T> : InvocationHandler
    {
        public object InvokeMember(object sender, int methodId, string name, params object[] args)
        {
            //服务端调用的时候会用
            var met = (MethodInfo)typeof(T).Module.ResolveMethod(methodId);
            string[] names = name.Split('+');
            var NameSpace = names[0];
            var Method = names[1];
            var Parameters = new List<object>(args);
            Console.WriteLine($"{NameSpace}.{Method}({string.Join(",", Parameters)})");
            if (met.ReturnType.IsValueType)
            {
                return 0;
            }
            return null;
        }
    }
}
