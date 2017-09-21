using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nemerle.Compiler;
using Nemerle.Completion2;

namespace NemerleServer
{
    public class CompletionProject : Nemerle.Completion2.IIdeProject
    {
        private List<string> _references;
        private List<string> _macroReferences;
        private List<KeyValuePair<string, FileNemerleSource>> _sources;
        private List<string> _constants;

        public CompletionProject(NemerleProject project)
        {
            _sources = project
                .SourceFiles
                .Select(x => GetPair(project.ProjectFilePath, x))
                .ToList();
            _references = project
                .References
                .Where(x => x.RequiredTargetFramework == null || x.RequiredTargetFramework <= project.TargetFrameworkVersion)
                .Select(x => (x.HintPath == null) ? x.Include : GetAbsolutePath(project.ProjectFilePath, x.HintPath))
                .ToList();
            _macroReferences = project
                .MacroReferences
                .Where(x => x.RequiredTargetFramework == null || x.RequiredTargetFramework <= project.TargetFrameworkVersion)
                .Select(x => (x.HintPath == null) ? x.Include : GetAbsolutePath(project.ProjectFilePath, x.HintPath))
                .ToList();
            _constants = project
                .CompilationSymbols
                .ToList();
        }

        public bool IsLoaded
        {
            get
            {
                return true;
            }
        }

        public void AddOverrideMembers(IIdeSource source, TypeBuilder ty, IEnumerable<IMember> notOverriden)
        {
        }

        public void AddUnimplementedMembers(IIdeSource source, TypeBuilder ty, IEnumerable<IGrouping<FixedType.Class, IMember>> unimplementedMembers)
        {
        }

        public void ClearAllCompilerMessages()
        {
        }

        public void ClearMethodCompilerMessages(MemberBuilder member)
        {
        }

        public IEnumerable<string> GetAssemblyReferences()
        {
            return _references;
        }

        private static string GetAbsolutePath(string projectFilePath, string relativePath)
        {
            var directory = Path.GetDirectoryName(projectFilePath);
            var absPath = Path.Combine(directory, relativePath);
            return Path.GetFullPath(absPath);
        }

        public IEnumerable<string> GetMacroAssemblyReferences()
        {
            return _macroReferences;
        }

        public CompilationOptions GetOptions()
        {
            var options = new CompilationOptions
            {
                GreedyReferences = false,
                ColorMessages = false,
                IgnoreConfusion = true,
            };
            foreach (var constant in _constants)
            {
                options.DefineConstant(constant);
            }
            return options;
        }

        public IIdeSource GetSource(int fileIndex)
        {
            return _sources.Select(x => x.Value).FirstOrDefault(x => x.FileIndex == fileIndex);
        }

        public IEnumerable<IIdeSource> GetSources()
        {
            return _sources.Select(x => x.Value);
        }

        public GotoInfo[] LookupLocationsFromDebugInformation(GotoInfo info)
        {
            throw new NotImplementedException();
        }

        public void SetCompilerMessageForCompileUnit(CompileUnit compileUnit)
        {
        }

        public void SetMethodCompilerMessages(MemberBuilder member, IEnumerable<CompilerMessage> messages)
        {
        }

        public void SetStatusText(string text)
        {
        }

        public void SetTopLevelCompilerMessages(IEnumerable<CompilerMessage> messages)
        {
        }

        public void SetUsageHighlighting(IIdeSource source, IEnumerable<GotoInfo> usages)
        {
        }

        public void ShowMessage(string message, MessageType messageType)
        {
        }

        public void TypesTreeCreated()
        {
        }

        public FileNemerleSource GetSource(string path)
        {
            return _sources.FirstOrDefault(x => x.Key == path).Value;
        }

        private static KeyValuePair<string, FileNemerleSource> GetPair(string projectFile, string sourceFile)
        {
            var projectDirectory = Path.GetDirectoryName(projectFile);
            var path = Path.GetFullPath(Path.Combine(projectDirectory, sourceFile));
            var source = new FileNemerleSource(Location.GetFileIndex(path));
            return new KeyValuePair<string, FileNemerleSource>(path, source);
        }

    }
}
