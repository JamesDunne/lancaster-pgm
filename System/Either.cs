using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public struct Either<L, R>
    {
        readonly L _l;
        readonly R _r;
        bool _isRight;

        public Either(L l)
        {
            _isRight = false;
            _l = l;
            _r = default(R);
        }

        public Either(R r)
        {
            _isRight = true;
            _l = default(L);
            _r = r;
        }

        public L Left { get { if (_isRight) throw new Exception(); else return _l; } }
        public R Right { get { if (!_isRight) throw new Exception(); else return _r; } }

        public bool IsLeft { get { return !_isRight; } }
        public bool IsRight { get { return _isRight; } }

        public U Collapse<U>(Func<L, U> isLeft, Func<R, U> isRight)
        {
            if (_isRight)
                return isRight(_r);
            else
                return isLeft(_l);
        }

        public static implicit operator Either<L, R>(L left)
        {
            return new Either<L, R>(left);
        }

        public static implicit operator Either<L, R>(R right)
        {
            return new Either<L, R>(right);
        }
    }
}
