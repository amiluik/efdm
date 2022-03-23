﻿using EFDM.Abstractions.Audit;
using System;
using System.Collections.Generic;

namespace EFDM.Core.Audit {

    public class AuditSettings : IAuditSettings {
        public bool Enabled { get; set; }
        public HashSet<string> GlobalIgnoredProperties { get; set; } = new HashSet<string>();
        public HashSet<Type> IncludedTypes { get; set; } = new HashSet<Type>();
        public Dictionary<Type, List<int>> ExcludedTypeStateActions { get; set; } = new Dictionary<Type, List<int>>();
        public Dictionary<Type, HashSet<string>> IgnoredTypeProperties { get; set; } = new Dictionary<Type, HashSet<string>>();
    }
}
