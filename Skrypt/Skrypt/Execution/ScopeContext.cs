﻿using System;
using System.Collections.Generic;
using System.Text;
using Skrypt.Library;
using Newtonsoft.Json;

namespace Skrypt.Execution {
    public class SubContext {
        public bool InLoop = false;
        public bool InMethod = false;
        public UserMethod Method = null;
        public SkryptObject ReturnObject = null;
    }

    public class ScopeContext {
        public string Type = "";
        public SubContext subContext = new SubContext();
        public Dictionary<string, SkryptObject> Variables { get; set; } = new Dictionary<string, SkryptObject>();
        public ScopeContext ParentScope = null;

        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented).Replace("\"", "");
        }
    }
}

