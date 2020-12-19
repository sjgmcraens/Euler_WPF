using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    public static class MyExtensions
    {
        public static long[] CumSum(this long[] x)
        {
            for (int i = 1; i < x.Length; i++)
            {
                x[i] += x[i - 1];
            }
            return x;
        }

        public static long[] CumCumSum(this long[] x, int n)
        {
            for (int i = 0; i < n; i++)
            {
                x = x.CumSum();
            }
            return x;
        }

    }
}