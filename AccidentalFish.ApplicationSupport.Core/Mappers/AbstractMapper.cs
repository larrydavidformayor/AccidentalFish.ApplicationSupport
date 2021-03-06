﻿using System.Collections.Generic;
using System.Linq;

namespace AccidentalFish.ApplicationSupport.Core.Mappers
{
    public abstract class AbstractMapper<T1,T2> : IMapper<T1, T2>
    {
        public abstract T2 Map(T1 @from);
        
        public virtual IEnumerable<T2> Map(IEnumerable<T1> @from)
        {
            if (@from == null) return new T2[0];
            return @from.Select(Map).ToArray();
        }

        public abstract T1 Map(T2 @from);

        public virtual IEnumerable<T1> Map(IEnumerable<T2> @from)
        {
            if (@from == null) return new T1[0];
            return @from.Select(Map).ToArray();
        }
    }
}
