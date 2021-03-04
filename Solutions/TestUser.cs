using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Problems;

namespace Euler_WPF.Solutions
{
    static class TestUser
        /// Solutions created by TestUser
    {
        public static int Solution_1()
        {
            //Find the sum of all the multiples of 3 or 5 below 1000.
            int upperLimit = 1000;
            int sum = 0;
            int[] divisors = new int[] { 3, 5 };

                    foreach (int d in divisors)
                    {
                        int m = 1;
                        for (int prod = m* d; prod<upperLimit; prod = m* d)
                        {
                            sum += prod;
                            m++;
                        }
            }
            return sum;
        }
    }
}
