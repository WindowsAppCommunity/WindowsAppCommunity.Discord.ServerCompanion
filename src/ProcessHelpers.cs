using System.Diagnostics;

namespace WindowsAppCommunity.Discord.ServerCompanion
{
    /// <summary>
    /// Helper for interacting with executable binaries.
    /// </summary>
    public static class ProcessHelpers
    {
        /// <summary>
        /// Runs the provided executable with the given arguments.
        /// </summary>
        /// <param name="filePath">The path to the binary file to execute.</param>
        /// <param name="stdin">The stream to pipe as standard input to the child process.</param>
        /// <param name="arguments">The execution arguments to provide.</param>
        /// <param name="ct">A token that can be used to cancel the ongoing operation.</param>
        /// <exception cref="System.InvalidOperationException">An error was output during execution.</exception>
        public static async Task<Process> StartExecutableAsync(string filePath, Stream stdin, string arguments, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var processStartInfo = new ProcessStartInfo(filePath, arguments)
            {
                CreateNoWindow = true,
            };

            var proc = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            proc.Start();

            await stdin.CopyToAsync(proc.StandardInput.BaseStream, ct);
            
            return proc;
        }
    }
}