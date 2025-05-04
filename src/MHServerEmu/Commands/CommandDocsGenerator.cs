using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Commands
{
    internal static class CommandDocsGenerator
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        internal static void GenerateDocs(IEnumerable<CommandGroup> commandGroups, string outputPath)
        {
            Logger.Info("Generating command docs...");

            using StreamWriter writer = new(outputPath);

            writer.WriteLine("# Server Commands");
            writer.WriteLine();
            writer.WriteLine($"This list was automatically generated on `{DateTime.UtcNow:yyyy.MM.dd HH:mm:ss} UTC` using server version `{AssemblyHelper.GetAssemblyInformationalVersion()}`.");
            writer.WriteLine();
            writer.WriteLine("To see an up to date list of all commands, type !commands in the server console or the in-game chat. When invoking a command from in-game your account has to meet the user level requirement for the command.");
            writer.WriteLine();

            List<CommandGroup> singleCommandGroups = new();

            // Multi-command groups
            foreach (CommandGroup commandGroup in commandGroups.OrderBy(group => group.GroupDefinition.Name))
            {
                CommandGroupDefinition groupDefinition = commandGroup.GroupDefinition;

                // Single command groups are processed separately later
                if (groupDefinition.Flags.HasFlag(CommandGroupFlags.SingleCommand))
                {
                    singleCommandGroups.Add(commandGroup);
                    continue;
                }

                Logger.Info($"Adding {groupDefinition}...");

                const string CommandGroupSuffix = "Commands";

                string sectionName = groupDefinition.ToString();
                if (sectionName.EndsWith(CommandGroupSuffix))
                    sectionName = sectionName[..^CommandGroupSuffix.Length];

                writer.WriteLine($"## {sectionName}");
                writer.WriteLine(groupDefinition.Help);
                writer.WriteLine();

                TableBuilder tableBuilder = new("Command", "Description", "User Level", "Invoker Type");
                foreach (CommandDefinition commandDefinition in commandGroup.CommandDefinitions.OrderBy(commandDefinition => commandDefinition.Name))
                {
                    if (commandDefinition.IsDefaultCommand)
                        continue;

                    // Include usage string if the command has one
                    string name = string.IsNullOrWhiteSpace(commandDefinition.Usage) == false
                        ? $"{CommandManager.CommandPrefix}{commandDefinition.Usage}"
                        : $"{CommandManager.CommandPrefix}{groupDefinition.Name} {commandDefinition.Name}";

                    string description = commandDefinition.Description;

                    var userLevelValue = groupDefinition.UserLevel;
                    if (commandDefinition.UserLevel > userLevelValue)
                        userLevelValue = commandDefinition.UserLevel;
                    string userLevel = userLevelValue > 0 ? userLevelValue.ToString() : "Any";

                    string invokerType = commandDefinition.InvokerType.ToString();

                    tableBuilder.AddRow(name, description, userLevel, invokerType);
                }

                writer.WriteLine(tableBuilder.ToMarkdown());
            }

            // Single command groups
            {
                Logger.Info($"Adding misc commands...");

                writer.WriteLine($"## Misc");
                writer.WriteLine();

                TableBuilder tableBuilder = new("Command", "Description", "User Level", "Invoker Type");
                foreach (CommandGroup commandGroup in singleCommandGroups)
                {
                    CommandGroupDefinition groupDefinition = commandGroup.GroupDefinition;
                    CommandDefinition commandDefinition = commandGroup.CommandDefinitions.First();

                    string name = $"{CommandManager.CommandPrefix}{groupDefinition.Name}";

                    string description = groupDefinition.Help;

                    var userLevelValue = groupDefinition.UserLevel;
                    if (commandDefinition.UserLevel > userLevelValue)
                        userLevelValue = commandDefinition.UserLevel;
                    string userLevel = userLevelValue > 0 ? userLevelValue.ToString() : "Any";

                    string invokerType = commandDefinition.InvokerType.ToString();

                    tableBuilder.AddRow(name, description, userLevel, invokerType);
                }

                writer.WriteLine(tableBuilder.ToMarkdown());
            }

            Logger.Info("Finished generating command docs.");
        }

        private class TableBuilder
        {
            private readonly int _numColumns;
            private readonly List<string[]> _rows;

            public TableBuilder(params string[] columns)
            {
                _numColumns = columns.Length;
                _rows = [columns];
            }

            public void AddRow(params string[] row)
            {
                if (row.Length != _numColumns)
                    throw new InvalidDataException("The number of items in a row does not match the number of columns.");

                _rows.Add(row);
            }

            public string ToMarkdown()
            {
                StringBuilder sb = new();

                // Determine the width of each column based on the maximum item length.
                int[] columnWidths = new int[_numColumns];
                foreach (string[] row in _rows)
                {
                    for (int i = 0; i < row.Length; i++)
                        columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
                }

                // Format and write all rows
                bool isHeaderRow = true;
                foreach (string[] row in _rows)
                {
                    for (int i = 0; i < row.Length; i++)
                    {
                        string item = row[i];

                        sb.Append("| ");
                        sb.Append(FormatItem(item, columnWidths[i]));
                        sb.Append(' ');
                    }

                    sb.AppendLine(" |");

                    if (isHeaderRow)
                    {
                        for (int i = 0; i < _numColumns; i++)
                        {
                            sb.Append("| ");
                            sb.Append('-', columnWidths[i]);
                            sb.Append(' ');
                        }

                        sb.AppendLine(" |");

                        isHeaderRow = false;
                    }
                }

                return sb.ToString();
            }

            private static string FormatItem(string item, int width)
            {
                StringBuilder sb = new(item);
                sb.Replace('\n', ' ');
                sb.Replace('|', '/');       // | chars in descriptions are interpreted as cell ends, so we need to replace them
                sb.Append(' ', width - sb.Length);
                return sb.ToString();
            }
        }
    }
}
