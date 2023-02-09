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