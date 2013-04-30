using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public struct Maybe<T>
    {
        readonly T _value;
        readonly bool _hasValue;

        public T Value { get { if (!_hasValue) throw new Exception(); else return _value; } }
        public bool HasValue { get { return _hasValue; } }

        public Maybe(T value)
        {
            _value = value;
            _hasValue = true;
        }

        public U Collapse<U>(Maybe<T> maybe, Func<T, U> action, Func<U> noaction)
        {
            if (maybe.HasValue) return action(maybe.Value);
            else return noaction();
        }

        public static readonly Maybe<T> Nothing = new Maybe<T>();

        public static implicit operator Maybe<T>(T value)
        {
            return new Maybe<T>(value);
        }
    }
}
