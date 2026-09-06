#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MiningSimulator.Editor
{
    public sealed class MiningGitWindow : EditorWindow
    {
        private Vector2 scroll;
        private string commitMessage = "Update mining game";
        private string output = "Press Refresh Status to inspect the repository.";
        private bool isRunning;

        [MenuItem("Mining Simulator/Git/Repository Tool")]
        public static void Open()
        {
            GetWindow<MiningGitWindow>("Mining Git");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mining Simulator Git", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs Git in this Unity project's root. Commit & Push stages all project changes, " +
                "creates a commit, then pushes the current branch.", MessageType.Info);

            using (new EditorGUI.DisabledScope(isRunning))
            {
                if (GUILayout.Button("Refresh Status"))
                {
                    RunSequence(new GitCommand("status", "--short", "--branch"));
                }

                EditorGUILayout.Space();
                commitMessage = EditorGUILayout.TextField("Commit message", commitMessage);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Pull (fast-forward only)"))
                    {
                        RunSequence(new GitCommand("pull", "--ff-only"));
                    }

                    if (GUILayout.Button("Push"))
                    {
                        RunSequence(new GitCommand("push"));
                    }
                }

                GUI.enabled = !isRunning && !string.IsNullOrWhiteSpace(commitMessage);
                if (GUILayout.Button("Commit All & Push", GUILayout.Height(34)) &&
                    EditorUtility.DisplayDialog("Commit All & Push",
                        "Stage every non-ignored project change, create a commit, and push it to GitHub?",
                        "Commit & Push", "Cancel"))
                {
                    RunSequence(
                        new GitCommand("add", "-A"),
                        new GitCommand("commit", "-m", commitMessage.Trim()),
                        new GitCommand("push"));
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(isRunning ? "Running..." : "Output", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(output, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private async void RunSequence(params GitCommand[] commands)
        {
            if (isRunning)
            {
                return;
            }

            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, ".git")))
            {
                output = "This Unity project is not inside a Git repository.";
                return;
            }

            isRunning = true;
            output = string.Empty;
            Repaint();

            try
            {
                foreach (GitCommand command in commands)
                {
                    GitResult result = await Task.Run(() => Execute(root, command));
                    output += result.DisplayText;

                    bool nothingToCommit = command.Name == "commit" &&
                        result.ExitCode != 0 &&
                        result.CombinedOutput.IndexOf("nothing to commit", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (result.ExitCode != 0 && !nothingToCommit)
                    {
                        output += $"\nStopped because git {command.Name} failed (exit {result.ExitCode}).\n";
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                output += $"\nGit tool error: {exception.Message}\n";
                Debug.LogException(exception);
            }
            finally
            {
                isRunning = false;
                Repaint();
            }
        }

        private static GitResult Execute(string root, GitCommand command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = BuildArguments(command),
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

            using Process process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Could not start Git. Confirm Git is installed and available in PATH.");
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new GitResult(command, process.ExitCode, standardOutput, standardError);
        }

        private static string BuildArguments(GitCommand command)
        {
            var parts = new List<string> { Quote(command.Name) };
            foreach (string argument in command.Arguments)
            {
                parts.Add(Quote(argument));
            }
            return string.Join(" ", parts);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private readonly struct GitCommand
        {
            public GitCommand(string name, params string[] arguments)
            {
                Name = name;
                Arguments = arguments;
            }

            public string Name { get; }
            public IReadOnlyList<string> Arguments { get; }
        }

        private readonly struct GitResult
        {
            public GitResult(GitCommand command, int exitCode, string standardOutput, string standardError)
            {
                Command = command;
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public GitCommand Command { get; }
            public int ExitCode { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }
            public string CombinedOutput => StandardOutput + StandardError;
            public string DisplayText =>
                $"> git {Command.Name} {string.Join(" ", Command.Arguments)}\n" +
                StandardOutput + StandardError + $"[exit {ExitCode}]\n\n";
        }
    }
}
#endif
