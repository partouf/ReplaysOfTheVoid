namespace ReplaysOfTheVoid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PythonHelpers
    {
        public static string Conv2String(dynamic value)
        {
            if (value is IronPython.Runtime.Bytes)
            {
                IronPython.Runtime.Bytes v = value;
                return Encoding.UTF8.GetString(v.ToByteArray());
            }
            else if (value is string)
            {
                // redo encoding to unicode, was assumed to be ANSI while string was UTF8
                string v = value;
                byte[] arr = new byte[v.Length];
                for (var i = 0; i < v.Length; i++)
                {
                    arr[i] = (byte)v[i];
                }

                return Encoding.UTF8.GetString(arr);
            }
            else
            {
                throw new Exception("unknown type");
            }
        }
    }
}
