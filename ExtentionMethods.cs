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

        public static string RemoveAll(this string s, char c)
        {
            string r = "";
            foreach (string i in s.Split(c))
            {
                r += i;
            }
            return r;
        }

        public static int ToInt(this char c)
        {
            return (int)(c - '0');
        }

        public static string GetBetween(this string s, string sBefore, string sAfter)
        {
            if (s.Contains(sBefore) && s.Contains(sAfter))
            {
                int sBeforeI = s.IndexOf(sBefore, 0) + sBefore.Length;
                int sAfterI = s.IndexOf(sAfter, sBeforeI);
                return s.Substring(sBeforeI, sAfterI - sBeforeI);
            }
            return "";
        }

        public static string GetBetweenInclusive(this string str, string start, string end)
        {
            // Contains exception
            if (!str.Contains(start) || !str.Contains(end))
            {
                throw new Exception("GetBetweenInclusive() start or end missing exception.");
            }

            // Get start + end indexes
            int startI = str.IndexOf(start, 0);
            int endI = str.IndexOf(end, startI+start.Length);

            // End before start exception
            if (endI == -1)
            {
                throw new Exception("GetBetweenInclusive() end before start exception.");
            }

            string r = str.Substring(startI, endI - startI + end.Length);

            // Return
            return r;
        }
    }
}