using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functions
{
    public static class Prime
    // This class contains a list of the known primes and related methods.
    {
        public static List<long> Primes;
        public static long largestChecked;

        // Sieve
        public static List<bool> sieve;

        // Constructor
        static Prime()
        {
            // Add the first prime
            Primes = new List<long>() { 2 };
            largestChecked = 2;

            // Initialize sieve with 0,1,2 being sieved
            sieve = new List<bool> { false, false, false };
        }

        public static long GetPrimeAtIndex(int i)
        {
            if (Primes.Count < i)
            {
                CalcPrimes_UpToAndInc_Index(i);
            }
            return Primes[i];
        }

        public static void CalcPrimes_UpToAndInc_Index(int i)
        {
            if (Primes.Count < i)
            {

            }
        }

        public static Dictionary<int, int> GetPrimeFactors(long n)
        {
            // Create List
            Dictionary<int, int> factors = new Dictionary<int, int>();

            // Calculate primes up to sqrt(n)
            CalcPrimes_UpToAndInc_N(n);

            // This loop keeps dividing n by it's lowest prime factor untill n==1
            while (true)
            {
                // Look for factor and divide n by it
                foreach (int p in Primes)
                {
                    if (n % p == 0) // if divisor
                    {
                        // Divide the number and add the factor to the list of factors.
                        n /= p;

                        if (factors.ContainsKey(p))
                        {
                            factors[p]++;
                        }
                        else
                        {
                            factors.Add(p, 1);
                        }

                        break;
                    }
                }

                // If n is now 1, that means all factors were found
                if (n == 1)
                {
                    return factors;
                }
            }
        }

        public static void CalcPrimes_UpToAndInc_N(long n)
        {
            // Trivial
            if (largestChecked >= n)
            {
                return;
            }

            // Expand sieve
            for (int i = (int)largestChecked + 1; i <= n; i++)
            {
                sieve.Insert(i, true);
            }

            // Sqrt(n) rounded down.
            int sqrtN = (int)Math.Sqrt(n);

            // Go over the known primes and sieve their multiples
            foreach (int P in Primes)
            {
                // the lowest non-filtered multiplier is going to be largestChecked+1.0)/P rounded up.
                // Im going to <= n since we include n.
                for (int M = (int)Math.Ceiling((largestChecked + 1.0) / P) * P; M <= n; M += P)
                {
                    // Sieve
                    sieve[M] = false;
                }
            }

            // Check all prime candidates (largestChecked < P <= n)
            for (long P = largestChecked + 1; P <= n; P++)
            {
                if (sieve[(int)P]) // If not sieved (prime)
                {
                    Primes.Add(P);

                    // First composite remaining will be P*P
                    for (long M = P * P; M <= n; M += P)
                    {
                        // Sieve
                        sieve[(int)M] = false;
                    }
                }
            }
            largestChecked = n;
        }
    }

    class Misc
    {
        #region Static classes



        
        #endregion

        #region Misc functions

        // Dictionairy for keeping track of divisors
        public static Dictionary<int, SortedSet<int>> divisorsOfN = new Dictionary<int, SortedSet<int>>()
        {
            { 1, new SortedSet<int>(){ 1 } },
            { 2, new SortedSet<int>(){ 1,2 } }
        };
        public static void CalcDivisorsOfN(int n)
        {

            // Create new list and include 1 and n
            divisorsOfN[n] = new SortedSet<int>() { 1, n };

            // Get prime factors
            Dictionary<int, int> primeFactors = Prime.GetPrimeFactors(n);
            int[] pBases = primeFactors.Keys.ToArray();

            genDivisors(primeFactors, pBases, 0, 1, n);
        }

        public static void genDivisors(Dictionary<int, int> pF, int[] pBases, int index, int div, int n)
        {
            // Base case i.e. we do not have more 
            // primeFactors to include 
            if (index == pBases.Length)
            {
                divisorsOfN[n].Add(div);
                return;
            }

            for (int i = 0; i <= pF[pBases[index]]; ++i)
            {
                genDivisors(pF, pBases, index + 1, div, n);
                div *= pBases[index];
            }
        }

        public static void genDivisors(int primeFactorI, int divisor, Dictionary<int, int> primeFactors)
        {
            /// Recusive function that generates the divisors of a number 
            /// from its prime factors and stores them in the divisorsOfN dictionairy

            // For an amount of times equal to (the power of the factor + 1)
            for (int i = 0; i <= primeFactors[primeFactorI]; i++)
            {
                genDivisors(divisor + 1, divisor, primeFactors);
                divisor *= primeFactors[primeFactorI];
            }
        }

        public static long[] getCollatz(long n)
        {
            List<long> r = new List<long>();

            while (n > 1)
            {
                // Add step
                r.Add(n);

                // The collatz choice
                if (n % 2 == 0)
                {
                    n /= 2;
                }
                else
                {
                    n *= 3;
                    n += 1;
                }
            }

            r.Add(1);

            return r.ToArray();
        }

        public static string ReverseString(string s)
        // This function reverses a string
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static int GetLargestPalinDromeBelow(int n_int)
        // This function gives the largest palindrome below n (including n) Example: 99812
        {
            string n_str = n_int.ToString(); // Ex: "99812"
            bool lengthIsEven = n_str.Length % 2 == 0; // Ex: False
            // We determine the different parts of the number. Ex: 99, 8, 12
            string firstHalf_str = n_str.Substring(0, n_str.Length / 2); // This should floor the fraction. Ex: "99" 
            int firstHalf_int = Int32.Parse(firstHalf_str); // Ex: 99
            string secondHalf_str = n_str.Substring(n_str.Length - firstHalf_str.Length); // Ex: "12"
            int secondHalf_int = Int32.Parse(secondHalf_str); // Ex: 12

            //Console.WriteLine(firstHalf_str + " " + secondHalf_str);

            if (firstHalf_str == ReverseString(secondHalf_str))
            // If the second half reversed equals the first half, the number is a palindrome
            {
                //Console.WriteLine("Debug, 1");
                return n_int;
            }
            else if (secondHalf_int >= Int32.Parse(ReverseString(firstHalf_str)))
            // If the second half is >= than the reverse of the first half (Ex: 12>=99 ==> false), than finding the palingdrome is easy.
            // In the case of 32899 for example, the second half (99) can become the reverse of the first half (23).
            {
                //Console.WriteLine("Debug, 2");
                string newPalindrome = firstHalf_str;
                if (!lengthIsEven)
                {
                    newPalindrome += n_str.Length / 2; // Middle caracter is unchanged
                }
                return Int32.Parse(newPalindrome += ReverseString(firstHalf_str)); // Ex: 32899 ==> 32823
            }
            else
            // If the second half was smaller than the reverse of the first half, we take the first half including
            // the middle caracter, subtract 1, and fix the end to be the revers of the start.
            {
                //Console.WriteLine("Debug, 3");
                int firstHalfInclusive = Int32.Parse(n_str.Substring(0, n_str.Length / 2)); // Ex: 998
                int newFirstHalfInclusive = firstHalfInclusive - 1; // Ex: 997
                //Console.WriteLine("NewFirstHalfIncl: " + newFirstHalfInclusive);
                string newFirstHalfInclusive_str = newFirstHalfInclusive.ToString(); // Ex: "997"
                int subtract = 0; // Case: even
                if (!lengthIsEven)
                {
                    subtract = 1;
                }
                string newFirstHalf_str = newFirstHalfInclusive_str.Substring(0, newFirstHalfInclusive_str.Length - subtract); // Ex: "99"
                string newSecondHalf_str = ReverseString(newFirstHalf_str);
                string newPalindrome = newFirstHalfInclusive_str + newSecondHalf_str; // Ex: 99799
                return Int32.Parse(newPalindrome);
            }
        }

        public static long LowestCommonMultiple(List<int> ns)
        {
            // Return trivial answer
            if (ns.Count < 2)
            {
                return ns[0];
            }

            Dictionary<int, int> factorHighestCount = new Dictionary<int, int>();
            // For every n
            foreach (int n in ns)
            {
                // Get count of prime factors
                Dictionary<int, int> primeCount = Prime.GetPrimeFactors(n);
                foreach (int i in primeCount.Keys)
                {

                    if (factorHighestCount.ContainsKey(i))
                    {
                        if (factorHighestCount[i] < primeCount[i])
                        {
                            factorHighestCount[i] = primeCount[i];
                        }
                    }
                    else
                    {
                        factorHighestCount.Add(i, primeCount[i]);
                    }
                }
            }

            // Multiply factors
            int total = 1;
            foreach (int i in factorHighestCount.Keys)
            {
                total *= Convert.ToInt32(Math.Pow(i, factorHighestCount[i]));
            }
            return total;
        }

        public static Dictionary<int, int> GetCountDictFromArray(int[] array)
        {
            Dictionary<int, int> count = new Dictionary<int, int>();
            foreach (int i in array)
            {
                if (count.ContainsKey(i))
                {
                    count[i] += 1;
                }
                else
                {
                    count.Add(i, 1);
                }
            }
            return count;
        }

        public static int[] GetPythagorianTriplet(int m, int n, int k)
        // This function generates pythagorian triplets given natural numbers k, m and n
        // While m>n
        {
            int a = k * (m * m - n * n);
            int b = k * (2 * m * n);
            int c = k * (m * m + n * n);
            return new int[] { a, b, c };
        }

        public static int GetNumOfDivisors(int n)
        {
            int divisorAm = 1;

            foreach (int primeExp in Prime.GetPrimeFactors(n).Values)
            {
                divisorAm *= (primeExp + 1);
            }
            return divisorAm;
        }

        public static string IntArrayToString(int[] x, string lead, string intermediary, string end)
        {
            string s = lead;
            for (int i = 0; i < x.Length; i++)
            {
                s += $"{x[i]}";
                if (i != s.Length - 1)
                {
                    s += intermediary;
                }
            }
            s += end;
            return s;
        }

        

        static Dictionary<int, string> numbersInWords = new Dictionary<int, string>()
        {
            {0,"zero" },
            {1,"one" },
            {2,"two" },
            {3,"three" },
            {4,"four" },
            {5,"five" },
            {6,"six" },
            {7,"seven" },
            {8,"eight" },
            {9,"nine" },
            {10,"ten" },
            {11,"eleven" },
            {12,"twelve" },
            {13,"thirteen" },
            {14,"fourteen" },
            {15,"fifteen" },
            {16,"sixteen" },
            {17,"seventeen" },
            {18,"eighteen" },
            {19,"nineteen" },
            {20,"twenty" },
            {30,"thirty" },
            {40,"forty" },
            {50,"fifty" },
            {60,"sixty" },
            {70,"seventy" },
            {80,"eighty" },
            {90,"ninety" },
            {100,"one hundred" },
            {1000,"one thousand" },
            {1000000,"one million" },
            {1000000000,"one billion" },
        };
        public static string IntToVerboseString(int n)
        {
            /// Converts numbers into their spellings.
            /// Warning: Not guaranteed to work for n > 1000

            // Check to see wether n is included in the dictionary ==> return it if so.
            if (numbersInWords.ContainsKey(n))
            {
                return numbersInWords[n];
            }

            // p == The largest power of 10 that is <= n
            int p = (int)Math.Pow(10, n.ToString().Length - 1);

            // a == The first caracter of n
            int a = n / p;

            // b == The first caracter followed by as many 0's as long as b<n
            int b = a * p;

            string nStr;
            if (n < 100)
            {
                // Ex: (Ninety) (nine)
                nStr = IntToVerboseString(b) + "-" + IntToVerboseString(n - b);
            }
            else if (n < 1000)
            {
                // Ex: (One) (hundred) and (ninety nine)
                nStr = IntToVerboseString(a) + " " + IntToVerboseString(p).Split(' ')[1];
                if (n - b != 0)
                {
                    nStr += " and " + IntToVerboseString(n - b);
                }
            }
            else
            {
                // Ex: (One) (thousand) (one hundred and ninety nine)
                nStr = IntToVerboseString(a) + " " + IntToVerboseString(p).Split(' ')[1] + " " + IntToVerboseString(n - b);
            }
            numbersInWords.Add(n, nStr);
            return nStr;
        }

        #endregion
    }
}
