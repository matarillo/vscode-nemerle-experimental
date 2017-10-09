using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace NemerleServer
{
    public class NemerleProject
    {
        public static NemerleProject Load(XDocument nproj, IDictionary<string, string> props)
        {
            throw new NotImplementedException();
        }

        public string ProjectFilePath { get; internal set; }

        public string Configuration { get; internal set; }

        public string Platform { get; internal set; }

        public ReadOnlyCollection<string> CompilationSymbols { get; internal set; }

        public Version TargetFrameworkVersion { get; internal set; }

        public ReadOnlyCollection<Reference> References { get; internal set; }

        public ReadOnlyCollection<Reference> MacroReferences { get; internal set; }

        public ReadOnlyCollection<string> SourceFiles { get; internal set; }

        public class Reference
        {
            public string Include { get; private set; }
            public string HintPath { get; private set; }
            public Version RequiredTargetFramework { get; private set; }

            public Reference(string include)
            {
                this.Include = include;
            }
            public Reference(string include, Version requiredTargetFramework)
            {
                this.Include = include;
                this.RequiredTargetFramework = requiredTargetFramework;
            }
            public Reference(string include, string hintPath)
            {
                this.Include = include;
                this.HintPath = hintPath;
            }
            public Reference(string include, Version requiredTargetFramework, string hintPath)
            {
                this.Include = include;
                this.RequiredTargetFramework = requiredTargetFramework;
                this.HintPath = hintPath;
            }
        }
    }
}
