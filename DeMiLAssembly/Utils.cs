using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Utils
{
    public static IEnumerator<object> FlattenThrowableIEnumerator(IEnumerable<object> enumerator)
    {
        return FlattenThrowableIEnumerator(enumerator.GetEnumerator());
    }
    public static IEnumerator<object> FlattenThrowableIEnumerator(IEnumerator<object> enumerator)
    {
        while(true)
        {
            object current = null;
            Exception ex = null;
            try
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }
                current = enumerator.Current;
            } catch (Exception e)
            {
                ex = e;
            }

            if(ex != null)
            {
                yield return ex;
                yield break;
            }

            if(current is IEnumerator<object> currentEnumertor)
            {
                var e = FlattenThrowableIEnumerator(currentEnumertor);
                while (e.MoveNext()) yield return e.Current;
            } else
            {
                yield return current;
            }
        }
    }
}


public class Tuple<T, U>
{
    public T Item1 { get; private set; }
    public U Item2 { get; private set; }

    public Tuple(T item1, U item2)
    {
        Item1 = item1;
        Item2 = item2;
    }
    public override string ToString()
    {
        return $"Tuple({Item1}, {Item2})";
    }

}

public static class Tuple
{
    public static Tuple<Tx, Ux> Create<Tx, Ux>(Tx item1, Ux item2)
    {
        return new Tuple<Tx, Ux>(item1, item2);
    }
    public static Tuple<Tx, Tx> Create<Tx>(IEnumerable<Tx> enumerable)
    {
        List<Tx> lst = enumerable.Take(2).ToList();
        if (lst.Count < 2) throw new ArgumentException("Length of IEnumerable must be at least 2");
        return new Tuple<Tx, Tx>(lst[0], lst[1]);
    }
    public static bool TryCreate<Tx>(IEnumerable<Tx> enumerable, out Tuple<Tx, Tx> result)
    {
        try
        {
            result = Create(enumerable);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

}
