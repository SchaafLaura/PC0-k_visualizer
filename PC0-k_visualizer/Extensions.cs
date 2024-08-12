using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC0
{
    internal static class Util
    {
        public static Random RNG = new Random();
    }

    internal static class Extensions
    {
        //static Random rng = new Random();
        public static VariableList<T> RotateThrough<T>(this VariableList<T> list)
        {
            var L = new VariableList<T>();
            L.Add(list[list.Count - 1]);
            for (int i = 0; i < list.Count - 1; i++)
                L.Add(list[i]);
            return L;
        }

        public static int RandomIndex<T>(this HashSet<T> s)
        {
            return Util.RNG.Next(s.Count);
        }

        public static int RandomIndex<T>(this List<T> l)
        {
            return Util.RNG.Next(l.Count);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> s)
        {
            return s.Count() == 0;
        }

        public static void Shuffle<T>(this List<T> list)
        {
            for(int i = 0; i < list.Count; i++)
            {
                var index = Util.RNG.Next(list.Count);
                var val = list[index];
                list.RemoveAt(index);
                list.Add(val);
            }
        }


    }

    


}
