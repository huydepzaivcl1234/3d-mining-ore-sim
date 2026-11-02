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
                        RunSequence(CreatePushCommand());
                    }
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(commitMessage)))
                {
                    if (GUILayout.Button("Commit All & Push", GUILayout.Height(34)) &&
                        EditorUtility.DisplayDialog("Commit All & Push",
                            "Stage every non-ignored project change, create a commit, and push it to GitHub?",
                            "Commit & Push", "Cancel"))
                    {
                        RunSequence(
                            new GitCommand("add", "-A"),
                            new GitCommand("commit", "-m", commitMessage.Trim()),
                            CreatePushCommand());
                    }
                }
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
                string currentBranch = string.Empty;
                if (RequiresSafeBranch(commands))
                {
                    if (IsRebaseInProgress(root))
                    {
                        output = "Git đang rebase. Tool đã dừng để bảo vệ Scene, icon và asset của bạn.\n" +
                                 "Đóng Unity, mở PowerShell tại project, chạy: git status\n" +
                                 "Giải quyết conflict rồi chạy: git rebase --continue\n";
                        return;
                    }

                    GitResult conflictResult = await Task.Run(() => Execute(root,
                        new GitCommand("diff", "--name-only", "--diff-filter=U")));
                    if (conflictResult.ExitCode != 0 ||
                        !string.IsNullOrWhiteSpace(conflictResult.StandardOutput))
                    {
                        output = "Repository còn merge conflict. Tool sẽ không commit hoặc push.\n" +
                                 conflictResult.StandardOutput + conflictResult.StandardError +
                                 "\nChạy git status trong PowerShell để xem các file cần xử lý.\n";
                        return;
                    }

                    GitResult branchResult = await Task.Run(() => Execute(root,
                        new GitCommand("branch", "--show-current")));
                    currentBranch = branchResult.StandardOutput.Trim();
                    if (branchResult.ExitCode != 0 || string.IsNullOrWhiteSpace(currentBranch))
                    {
                        output = "Git đang ở detached HEAD nên tool đã dừng, không tạo commit hoặc push.\n" +
                                 "Chạy git status trong PowerShell và hoàn tất rebase trước.\n";
                        return;
                    }
                }

                foreach (GitCommand requestedCommand in commands)
                {
                    GitCommand command = requestedCommand.IsPushPlaceholder
                        ? CreateResolvedPushCommand(currentBranch)
                        : requestedCommand;
                    GitResult result = await Task.Run(() => Execute(root, command));
                    output += result.DisplayText;
                    Repaint();

                    bool nothingToCommit = command.Name == "commit" &&
                        result.ExitCode != 0 &&
                        result.CombinedOutput.IndexOf("nothing to commit", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (result.ExitCode != 0 && !nothingToCommit)
                    {
                        output += BuildFailureHelp(command, result.CombinedOutput);
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

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("Could not start Git. Confirm Git is installed and available in PATH.");
            }

            // Drain both redirected streams concurrently. Reading one stream completely before
            // the other can deadlock when Git produces enough output to fill the second buffer.
            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Task.WaitAll(standardOutputTask, standardErrorTask);
            return new GitResult(command, process.ExitCode, standardOutputTask.Result, standardErrorTask.Result);
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
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '\"' }) < 0)
            {
                return value;
            }

            // ProcessStartInfo.Arguments is a command-line string, not a shell command. Escape
            // quotes using the Windows command-line parsing rules so commit messages containing
            // quotes or trailing backslashes arrive at Git unchanged.
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('\"');
            int backslashCount = 0;

            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '\"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append('\"');
                    backslashCount = 0;
                    continue;
                }

                builder.Append('\\', backslashCount);
                backslashCount = 0;
                builder.Append(character);
            }

            builder.Append('\\', backslashCount * 2);
            builder.Append('\"');
            return builder.ToString();
        }

        private static GitCommand CreatePushCommand()
        {
            // The concrete destination is resolved only after verifying that HEAD belongs to a
            // real branch. This placeholder must never be passed directly to Git.
            return new GitCommand("push-current-branch");
        }

        private static GitCommand CreateResolvedPushCommand(string branch)
        {
            return new GitCommand("push", "--set-upstream", "origin",
                $"HEAD:refs/heads/{branch}");
        }

        private static bool RequiresSafeBranch(IReadOnlyList<GitCommand> commands)
        {
            foreach (GitCommand command in commands)
            {
                if (command.Name == "add" || command.Name == "commit" ||
                    command.Name == "pull" || command.IsPushPlaceholder)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRebaseInProgress(string root)
        {
            string gitDirectory = Path.Combine(root, ".git");
            return Directory.Exists(Path.Combine(gitDirectory, "rebase-merge")) ||
                   Directory.Exists(Path.Combine(gitDirectory, "rebase-apply"));
        }

        private static string BuildFailureHelp(GitCommand command, string combinedOutput)
        {
            if (command.Name != "push" && command.Name != "pull")
            {
                return string.Empty;
            }

            if (Contains(combinedOutput, "authentication failed") ||
                Contains(combinedOutput, "could not read Username") ||
                Contains(combinedOutput, "terminal prompts disabled") ||
                Contains(combinedOutput, "repository not found"))
            {
                return "\nGitHub login failed. Sign in to the correct GitHub account in Git " +
                       "Credential Manager or GitHub Desktop, then try again.\n";
            }

            if (Contains(combinedOutput, "non-fast-forward") || Contains(combinedOutput, "fetch first"))
            {
                return "\nThe remote branch has newer commits. Use Pull (fast-forward only), " +
                       "review the incoming changes, then push again.\n";
            }

            if (Contains(combinedOutput, "not a full refname") ||
                Contains(combinedOutput, "detached HEAD"))
            {
                return "\nGit không ở một branch hợp lệ. Chạy git status và hoàn tất rebase trước khi push.\n";
            }

            if (Contains(combinedOutput, "not a git repository"))
            {
                return "\nOpen the Unity project from inside the cloned Git repository.\n";
            }

            return string.Empty;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
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
            public bool IsPushPlaceholder => Name == "push-current-branch";
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
