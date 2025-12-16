using RobotSim.Commands;

namespace RobotSim;

/// <summary>
/// Parses command strings into ICommand objects.
/// </summary>
public class Parser
{
    /// <summary>
    /// Attempts to parse a command string into an ICommand.
    /// Returns true if parsing was successful, false otherwise.
    /// </summary>
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

        // Parse Direction
        var directionStr = argParts[2].Trim().ToUpperInvariant();
        Direction? direction = directionStr switch
        {
            "NORTH" => Direction.North,
            "EAST" => Direction.East,
            "SOUTH" => Direction.South,
            "WEST" => Direction.West,
            _ => null
        };

        if (direction == null)
        {
            return false;
        }

        command = new PlaceCommand(x, y, direction.Value);
        return true;
    }
}

