using RobotSim.Commands;

namespace RobotSim;

/// <summary>
/// Parses command strings into ICommand objects.
/// Supports case-insensitive parsing of PLACE, MOVE, LEFT, RIGHT, and REPORT commands.
/// </summary>
public class Parser
{
    /// <summary>
    /// Attempts to parse a command string into an ICommand.
    /// </summary>
    /// <param name="line">The command string to parse.</param>
    /// <param name="command">The parsed command if successful; otherwise null.</param>
    /// <returns>True if parsing was successful; otherwise false.</returns>
    public bool TryParse(string line, out ICommand? command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        // Trim and convert to uppercase for case-insensitive comparison
        var trimmed = line.Trim();
        var upper = trimmed.ToUpperInvariant();

        // Check for simple commands (no arguments)
        if (upper == "MOVE")
        {
            command = new MoveCommand();
            return true;
        }

        if (upper == "LEFT")
        {
            command = new LeftCommand();
            return true;
        }

        if (upper == "RIGHT")
        {
            command = new RightCommand();
            return true;
        }

        if (upper == "REPORT")
        {
            command = new ReportCommand();
            return true;
        }

        // Check for PLACE command
        if (upper.StartsWith("PLACE ") || upper.StartsWith("PLACE\t"))
        {
            return TryParsePlaceCommand(trimmed, out command);
        }

        // Unknown command
        return false;
    }

    private bool TryParsePlaceCommand(string line, out ICommand? command)
    {
        command = null;

        // Extract the arguments part after "PLACE "
        var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        var args = parts[1].Trim();

        // Split by comma: X,Y,F
        var argParts = args.Split(',');
        if (argParts.Length != 3)
        {
            return false;
        }

        // Parse X
        if (!int.TryParse(argParts[0].Trim(), out var x))
        {
            return false;
        }

        // Parse Y
        if (!int.TryParse(argParts[1].Trim(), out var y))
        {
            return false;
        }

        // Parse Direction - must be one of the four named directions (not numeric)
        var directionStr = argParts[2].Trim();
        
        // Reject numeric inputs (Enum.TryParse accepts "0", "1", "2", "3" as valid)
        if (int.TryParse(directionStr, out _))
        {
            return false;
        }
        
        if (!Enum.TryParse<Direction>(directionStr, ignoreCase: true, out var direction))
        {
            return false;
        }
        
        // Ensure the parsed direction is one of the four defined values
        if (!Enum.IsDefined(typeof(Direction), direction))
        {
            return false;
        }

        command = new PlaceCommand(x, y, direction);
        return true;
    }
}

