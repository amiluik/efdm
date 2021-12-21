﻿using EFDM.Abstractions.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDM.Core.Models.Domain {

    public abstract class DeletableEntityBase<TKey> : EntityBase<TKey>, IEntityBase<TKey>, IDeletableEntity
        where TKey : IComparable, IEquatable<TKey> {

        public bool IsDeleted { get; set; }
    }
}
