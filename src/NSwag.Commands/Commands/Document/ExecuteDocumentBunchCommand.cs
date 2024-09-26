using NConsole;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NSwag.Commands.Document
{
    [Command(Name = "runBatch", Description = "Executes an .nswag file. If 'input' is not specified then all *.nswag files and the nswag.json file is executed.")]
    public class ExecuteDocumentBunchCommand : IConsoleCommand
    {
		[Argument(Position = 1, IsRequired = false)]
		public string InputDirectory { get; set; }

        public async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (InputDirectory == null)
            {
                throw new ArgumentNullException(nameof(InputDirectory));
            }

            if (!Directory.Exists(InputDirectory)) 
            {
                throw new DirectoryNotFoundException(nameof(InputDirectory));
            }

            foreach(var nswgFilePath in  Directory.GetFiles(InputDirectory, "*.nswag"))
            {
                var cmd = new ExecuteDocumentCommand()
                {
                    Input = nswgFilePath
                };

                await cmd.RunAsync(processor, host);
            }

            return await Task.FromResult(Task.CompletedTask);

        }
    }
}