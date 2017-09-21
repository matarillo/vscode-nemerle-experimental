using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Nemerle.Completion2;
using WpfHint.Parsing;
using Msbuild = Microsoft.Build.Evaluation;

namespace NemerleServer
{
    public class NemerleEngine
    {
        public static Action<string> Logger;

        CompletionProject _project;
        IIdeEngine _engine;
        internal NemerleEngine(CompletionProject project, IIdeEngine engine)
        {
            var handle = engine.BeginReloadProject();
            handle.AsyncWaitHandle.WaitOne();
            _project = project;
            _engine = engine;
        }

        public Tuple<Tuple<int,int>, Tuple<int,int>, string> Hover(string filePath, int line, int col)
        {
            var src = _project.GetSource(filePath);
            var request = _engine.BeginGetQuickTipInfo(src, line + 1, col + 1);
            if (!request.AsyncWaitHandle.WaitOne(1000))
            {
                return null;
            }
            var info = request.QuickTipInfo;
            if (info == null)
            {
                return null;
            }
            var begin = Tuple.Create(info.Location.Line - 1, info.Location.Column - 1);
            var end = Tuple.Create(info.Location.EndLine - 1, info.Location.EndColumn - 1);
            var text = new StringBuilder();
            ToMarkDown(text, WpfHint.Parsing.HintParser.Parse("<root>" + info.Text + "</root>"));
            return Tuple.Create(begin, end, text.ToString());
        }

        public static NemerleEngine LoadFromWorkspace(string workspacePath)
        {
            var projFiles = Directory.GetFiles(workspacePath, "*.nproj", SearchOption.AllDirectories);
            if (projFiles == null || projFiles.Length != 1)
            {
                Logger?.Invoke("Nemerle Project File is not found.");
                return null;
            }
            var nproj = projFiles[0];
            var props = new Dictionary<string, string>()
            {
                { "NemerleBinPathRoot", @"E:\Shared\binary\Nemerle-v1.2.543.0" },
            };
            var p = Load(nproj, props);
            var cp = new CompletionProject(p);
            var engine = EngineFactory.Create(cp, TextWriter.Null, false);
            var result = new NemerleEngine(cp, engine);
            return result;
        }

        public static NemerleProject Load(string projectFilePath, IDictionary<string, string> globalProperties)
        {
            var project = new Msbuild.Project(projectFilePath, globalProperties, "4.0"); // ToolsVersion=="4.0"
            var compilationSymbols = ToCompilationSymbols(project.GetProperty("Configuration").EvaluatedValue);
            var targetFrameworkVersion = new Version(project.GetProperty("TargetFrameworkVersion").EvaluatedValue.Substring(1)); // skip "v"
            var references = project
                .GetItemsIgnoringCondition("Reference")
                .Select(x => ToReference(x))
                .ToList();
            var macroReferences = project
                .GetItemsIgnoringCondition("MacroReference")
                .Select(x => ToReference(x))
                .ToList();
            var sourceFiles = project
                .GetItemsIgnoringCondition("Compile")
                .Select(x => x.EvaluatedInclude)
                .ToList();
            return new NemerleProject
            {
                ProjectFilePath = projectFilePath,
                TargetFrameworkVersion = targetFrameworkVersion,
                CompilationSymbols = new ReadOnlyCollection<string>(compilationSymbols),
                References = new ReadOnlyCollection<NemerleProject.Reference>(references),
                MacroReferences = new ReadOnlyCollection<NemerleProject.Reference>(macroReferences),
                SourceFiles = new ReadOnlyCollection<string>(sourceFiles)
            };
        }

        private static IList<string> ToCompilationSymbols(string defineConstants)
        {
            var list = new List<string>();
            if (defineConstants != null)
            {
                foreach (var constant in defineConstants.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = constant.Trim();
                    if (!string.IsNullOrEmpty(constant))
                    {
                        list.Add(constant.Trim());
                    }
                }
            }
            return list;
        }

        private static NemerleProject.Reference ToReference(Msbuild.ProjectItem item)
        {
            var req = item.DirectMetadata.FirstOrDefault(y => y.Name == "RequiredTargetFramework");
            var hint = item.DirectMetadata.FirstOrDefault(y => y.Name == "HintPath");
            return
                (req == null && hint == null) ? new NemerleProject.Reference(item.EvaluatedInclude) :
                (req != null && hint == null) ? new NemerleProject.Reference(item.EvaluatedInclude, new Version(req.EvaluatedValue)) :
                (req == null && hint != null) ? new NemerleProject.Reference(item.EvaluatedInclude, hint.EvaluatedValue) :
                new NemerleProject.Reference(item.EvaluatedInclude, new Version(req.EvaluatedValue), hint.EvaluatedValue);
        }

        private static void ToMarkDown(StringBuilder builder, ParseToken token)
        {
            TextToken tt;
            ElementToken et;
            if ((tt = token as TextToken) != null)
            {
                builder.Append(Escape(token.Text));
            }
            else if ((et = token as ElementToken) != null)
            {
                switch (et.Name)
                {
                    case "i":
                        builder.Append("*");
                        break;
                    case "b":
                        builder.Append("**");
                        break;
                    case "lb":
                        builder.Append(Environment.NewLine).Append(Environment.NewLine);
                        break;
                    case "hint":
                        builder.Append(et.Attributes["value"]);
                        break;
                }
                if (et.Name != "hint")
                {
                    foreach (var ct in token.Tokens)
                    {
                        ToMarkDown(builder, ct);
                    }
                }
                switch (et.Name)
                {
                    case "i":
                        builder.Append("*");
                        break;
                    case "b":
                        builder.Append("**");
                        break;
                }
            }
            else
            {
                foreach (var ct in token.Tokens)
                {
                    ToMarkDown(builder, ct);
                }
            }
        }
    
        private static string Escape(string raw)
        {
            //* # / ( ) [ ] < >
            var list = new List<char>(raw.Length * 2);
            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                switch (c)
                {
                    case '*':
                    case '#':
                    case '/':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case '<':
                    case '>':
                        list.Add('\\');
                        break;
                }
                list.Add(c);
            }
            return new string(list.ToArray());
        }
    }
}
