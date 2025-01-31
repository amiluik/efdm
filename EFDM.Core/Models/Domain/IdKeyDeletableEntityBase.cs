﻿using EFDM.Abstractions.Models.Domain;
using System;

namespace EFDM.Core.Models.Domain {

    public abstract class IdKeyDeletableEntityBase<TKey> : IdKeyEntityBase<TKey>, IDeletableEntity
        where TKey : IComparable, IEquatable<TKey> {

        public bool IsDeleted { get; set; }
    }
}
