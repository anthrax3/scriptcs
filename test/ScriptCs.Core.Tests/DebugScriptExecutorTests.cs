﻿using System;
using System.IO;
using System.Linq;
using Moq;
using ScriptCs.Contracts;
using Xunit;

namespace ScriptCs.Tests
{

    public class DebugScriptExecutorTests
    {
        public static DebugScriptExecutor CreateScriptExecutor(
            Mock<IFileSystem> fileSystem = null,
            Mock<IFilePreProcessor> fileProcessor = null,
            Mock<IScriptEngine> scriptEngine = null,
            Mock<IScriptHostFactory> scriptHostFactory = null)
        {
            if (fileSystem == null)
            {
                fileSystem = new Mock<IFileSystem>();
                fileSystem.Setup(fs => fs.GetWorkingDirectory(It.IsAny<string>())).Returns(@"C:\");
                fileSystem.Setup(fs => fs.CreateFileStream(It.IsAny<string>(), It.IsAny<FileMode>()))
                          .Returns(new MemoryStream());
            }

            fileProcessor = fileProcessor ?? new Mock<IFilePreProcessor>();

            if (scriptEngine == null)
            {
                var mockSession = new Mock<ISession>();
                mockSession.Setup(s => s.AddReference(It.IsAny<string>()));
                mockSession.Setup(s => s.Execute(It.IsAny<string>())).Returns(new object());

                scriptEngine = new Mock<IScriptEngine>();
                scriptEngine.SetupProperty(e => e.BaseDirectory);
                scriptEngine.Setup(e => e.CreateSession()).Returns(mockSession.Object);
                scriptEngine.Setup(e => e.CreateSession(It.IsAny<ScriptHost>())).Returns(mockSession.Object);
            }

            if (scriptHostFactory == null)
            {
                return new DebugScriptExecutor(fileSystem.Object, fileProcessor.Object, scriptEngine.Object);
            }
            else
            {
                return new DebugScriptExecutor(fileSystem.Object, fileProcessor.Object, scriptEngine.Object, scriptHostFactory.Object);
            }
        }

        public class TheExecuteMethod
        {
            [Fact]
            public void ShouldCompileProcessedCode()
            {
                // arrange
                var filePreProcessor = new Mock<IFilePreProcessor>();
                var scriptEngine = new Mock<IScriptEngine>();
                var session = new Mock<ISession>();
                var submission = new Mock<ISubmission<object>>();
                var compilation = new Mock<ICompilation>();

                const string PathToScript = @"C:\script.csx";
                var code = Guid.NewGuid().ToString();

                filePreProcessor.Setup(p => p.ProcessFile(PathToScript)).Returns(code);

                scriptEngine.Setup(e => e.CreateSession(It.IsAny<ScriptHost>())).Returns(session.Object);
                scriptEngine.SetupProperty(e => e.BaseDirectory);

                session.Setup(s => s.CompileSubmission<object>(code)).Returns(submission.Object).Verifiable();
                session.Setup(s => s.Engine).Returns(scriptEngine.Object);

                submission.Setup(s => s.Compilation).Returns(compilation.Object);

                var scriptExecutor = DebugScriptExecutorTests.CreateScriptExecutor(
                    scriptEngine: scriptEngine,
                    fileProcessor: filePreProcessor);

                // act
                scriptExecutor.Execute(PathToScript, Enumerable.Empty<string>(), Enumerable.Empty<IScriptPack>());

                // assert
                session.Verify(s => s.CompileSubmission<object>(code), Times.Once());
            }

            [Fact]
            public void ShouldEmitCompilationProvidingPathsForDllAndPdbFiles()
            {
                // arrange
                var scriptEngine = new Mock<IScriptEngine>();
                var session = new Mock<ISession>();
                var submission = new Mock<ISubmission<object>>();
                var compilation = new Mock<ICompilation>();

                var fileSystem = new Mock<IFileSystem>();

                const string PathToScript = @"C:\script.csx";
                const string BinDir = @"C:\bin";
                const string OutputDllName = "script.dll";
                const string OutputPdbName = "script.pdb";
                var pdbFullPath = Path.Combine(BinDir, OutputPdbName);
                var dllFullPath = Path.Combine(BinDir, OutputDllName);

                var pdbStream = new MemoryStream();
                var dllStream = new MemoryStream();

                scriptEngine.Setup(e => e.CreateSession(It.IsAny<ScriptHost>())).Returns(session.Object);
                scriptEngine.SetupProperty(e => e.BaseDirectory);

                session.Setup(s => s.CompileSubmission<object>(It.IsAny<string>())).Returns(submission.Object);
                session.Setup(s => s.Engine).Returns(scriptEngine.Object);

                submission.Setup(s => s.Compilation).Returns(compilation.Object);

                fileSystem.Setup(fs => fs.GetWorkingDirectory(PathToScript)).Returns(@"C:\");
                fileSystem.Setup(fs => fs.CreateFileStream(pdbFullPath, FileMode.OpenOrCreate)).Returns(pdbStream).Verifiable();
                fileSystem.Setup(fs => fs.CreateFileStream(dllFullPath, FileMode.OpenOrCreate)).Returns(dllStream).Verifiable();

                compilation.Setup(c => c.Emit(dllStream, pdbStream)).Verifiable();

                var scriptExecutor = DebugScriptExecutorTests.CreateScriptExecutor(fileSystem, scriptEngine: scriptEngine);

                // act
                scriptExecutor.Execute(PathToScript, Enumerable.Empty<string>(), Enumerable.Empty<IScriptPack>());

                // assert
                compilation.Verify(c => c.Emit(dllStream, pdbStream), Times.Once());
            }
        }
    }
}
