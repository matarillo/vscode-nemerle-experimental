using LanguageServer;
using LanguageServer.Client;
using LanguageServer.Json;
using LanguageServer.Parameters;
using LanguageServer.Parameters.General;
using LanguageServer.Parameters.TextDocument;
using LanguageServer.Parameters.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NemerleServer;
using System.Net;

namespace SampleServer
{
    public class App : ServiceConnection
    {
        private Uri _workerSpaceRoot;
        private int _maxNumberOfProblems;
        private TextDocumentManager _documents;

        private NemerleEngine _engine;

        public App(Stream input, Stream output, Action<string> trace)
            : base(input, output, trace)
        {
            _documents = new TextDocumentManager();
            _documents.Changed += Documents_Changed;
        }

        private void Documents_Changed(object sender, TextDocumentChangedEventArgs e)
        {
            ValidateTextDocument(e.Document);
        }

        protected override Result<InitializeResult, ResponseError<InitializeErrorData>> Initialize(InitializeParams @params)
        {
            _workerSpaceRoot = @params.rootUri;
            InitializeEngine();
            var result = new InitializeResult
            {
                capabilities = new ServerCapabilities
                {
                    textDocumentSync = TextDocumentSyncKind.Full,
                    completionProvider = new CompletionOptions
                    {
                        resolveProvider = true
                    },
                    hoverProvider = true
                }
            };
            return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success(result);
        }

        protected override void DidOpenTextDocument(DidOpenTextDocumentParams @params)
        {
            _documents.Add(@params.textDocument);
            Logger.Instance.Log($"{@params.textDocument.uri} opened.");
        }

        protected override void DidChangeTextDocument(DidChangeTextDocumentParams @params)
        {
            _documents.Change(@params.textDocument.uri, @params.textDocument.version, @params.contentChanges);
            Logger.Instance.Log($"{@params.textDocument.uri} changed.");
        }

        protected override void DidCloseTextDocument(DidCloseTextDocumentParams @params)
        {
            _documents.Remove(@params.textDocument.uri);
            Logger.Instance.Log($"{@params.textDocument.uri} closed.");
        }

        protected override void DidChangeConfiguration(DidChangeConfigurationParams @params)
        {
            _maxNumberOfProblems = @params?.settings?.languageServerExample?.maxNumberOfProblems ?? 100;
            foreach (var document in _documents.All)
            {
                ValidateTextDocument(document);
            }
        }

        private void ValidateTextDocument(TextDocumentItem document)
        {
            var diagnostics = new List<Diagnostic>();
            var lines = document.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var problems = 0;
            for (var i = 0; i < lines.Length && problems < _maxNumberOfProblems; i++)
            {
                var line = lines[i];
                var index = line.IndexOf("typescript");
                if (index >= 0)
                {
                    problems++;
                    diagnostics.Add(new Diagnostic
                    {
                        severity = DiagnosticSeverity.Warning,
                        range = new Range
                        {
                            start = new Position { line = i, character = index },
                            end = new Position { line = i, character = index + 10 }
                        },
                        message = $"{line.Substring(index, 10)} should be spelled TypeScript",
                        source = "ex"
                    });
                }
            }

            Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                uri = document.uri,
                diagnostics = diagnostics.ToArray()
            });
        }

        protected override void DidChangeWatchedFiles(DidChangeWatchedFilesParams @params)
        {
            Logger.Instance.Log("We received an file change event");
        }

        protected override Result<ArrayOrObject<CompletionItem, CompletionList>, ResponseError> Completion(TextDocumentPositionParams @params)
        {
            var array = new[]
            {
                new CompletionItem
                {
                    label = "TypeScript",
                    kind = CompletionItemKind.Text,
                    data = 1
                },
                new CompletionItem
                {
                    label = "JavaScript",
                    kind = CompletionItemKind.Text,
                    data = 2
                }
            };
            return Result<ArrayOrObject<CompletionItem, CompletionList>, ResponseError>.Success(array);
        }

        protected override Result<CompletionItem, ResponseError> ResolveCompletionItem(CompletionItem @params)
        {
            if (@params.data == 1)
            {
                @params.detail = "TypeScript details";
                @params.documentation = "TypeScript documentation";
            }
            else if (@params.data == 2)
            {
                @params.detail = "JavaScript details";
                @params.documentation = "JavaScript documentation";
            }
            return Result<CompletionItem, ResponseError>.Success(@params);
        }

        protected override Result<Hover, ResponseError> Hover(TextDocumentPositionParams @params)
        {
            try
            {
                var pos = @params.position;
                var url = @params.textDocument.uri;
                var tips = _engine?.Hover(ToLocalPath(url), (int)pos.line, (int)pos.character);
                var h = new Hover();
                if (tips != null)
                {
                    h.range = new Range
                    {
                        start = new Position { line = tips.Item1.Item1, character = tips.Item1.Item2 },
                        end = new Position { line = tips.Item2.Item1, character = tips.Item2.Item2 },
                    };
                    h.contents = (StringOrObject<MarkedString>)tips.Item3;
                }
                return Result<Hover, ResponseError>.Success(h);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.ToString());
                return Result<Hover, ResponseError>.Error(new ResponseError { code = ErrorCodes.InternalError, message = ex.Message });
            }
        }

        private void InitializeEngine()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                NemerleEngine.Logger = text => Logger.Instance.Info(text);
                try
                {
                    var ws = ToLocalPath(_workerSpaceRoot);
                    _engine = NemerleEngine.LoadFromWorkspace(ws);
                    if (_engine != null)
                    {
                        Logger.Instance.Info("Nemerle Engine is up.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex.ToString());
                }
            });
        }

        private Result<Hover, ResponseError> HoverFromDict(TextDocumentPositionParams @params)
        {
            if (dict == null)
            {
                dict = EnglishDictionary.Load();
            }

            var pos = @params.position;
            var url = @params.textDocument.uri;
            TextDocumentItem document;
            if (_documents.TryGetDocument(url, out document))
            {
                var line = document.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[pos.line];
                var character = (int)pos.character;
                if (IsOnTheWord(line, character))
                {
                    var left = character;
                    var right = character;
                    while (0 <= left && IsOnTheWord(line, left)) left--;
                    while (right < line.Length && IsOnTheWord(line, right)) right++;
                    var word = line.Substring(left + 1, right - left - 1);
                    List<string> list;
                    if (dict.TryGetValue(word, out list))
                    {
                        var h = new Hover
                        {
                            range = new Range
                            {
                                start = new Position { line = pos.line, character = left + 1L },
                                end = new Position { line = pos.line, character = right }
                            },
                            contents = list.Select(x => (StringOrObject<MarkedString>)x).ToArray()
                        };
                        return Result<Hover, ResponseError>.Success(h);
                    }
                }
            }
            return Result<Hover, ResponseError>.Success(new Hover());
        }

        private static bool IsOnTheWord(string line, int character)
        {
            var c = line[character];
            return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z');
        }

        SortedDictionary<string, List<string>> dict;

        public static string ToLocalPath(Uri url)
        {
            // WORKAROUND:
            // url.OriginalString may be URL-encoded, and that causes url.LocalPath not to work properly.
            var decodedUrl = new Uri(WebUtility.UrlDecode(url.OriginalString));
            return decodedUrl.LocalPath;
        }
    }
}
