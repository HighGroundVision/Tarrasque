using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque
{
    public static class MyExtensions
    {
        public static List<T> ToList<T>(this Tuple<T, T, T, T> tuple)
        {
            return new List<T>() { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 };
        }

        public static List<T> ToList<T>(this Tuple<T, T> tuple)
        {
            return new List<T>() { tuple.Item1, tuple.Item2 };
        }

    }
}
