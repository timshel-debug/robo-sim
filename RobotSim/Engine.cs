using RobotSim.Commands;

namespace RobotSim;

/// <summary>
/// The core engine that executes commands and manages robot state transitions.
/// This is pure/deterministic logic with no I/O.
/// </summary>
public class Engine
{
    private readonly Tabletop _tabletop;

    public Engine(Tabletop tabletop)
    {
        _tabletop = tabletop;
    }

    /// <summary>
    /// Executes a command and returns the new state and optional report output.
    /// </summary>
    public StepResult Execute(RobotState state, ICommand command)
    {
        return command switch
        {
            PlaceCommand place => ExecutePlace(state, place),
            MoveCommand => ExecuteMove(state),
            LeftCommand => ExecuteLeft(state),
            RightCommand => ExecuteRight(state),
            ReportCommand => ExecuteReport(state),
            _ => new StepResult(state, null) // Unknown command - ignore
        };
    }

    private StepResult ExecutePlace(RobotState state, PlaceCommand place)
    {
        var position = new Position(place.X, place.Y);
        
        // Validate that the placement is within bounds
        if (!_tabletop.IsValid(position))
        {
            // Invalid placement - ignore and keep current state
            return new StepResult(state, null);
        }

        // Place the robot at the specified position and direction
        var newState = new RobotState(true, position, place.Facing);
        return new StepResult(newState, null);
    }

    private StepResult ExecuteMove(RobotState state)
    {
        // If robot is not placed, ignore the command
        if (!state.IsPlaced || state.Pos == null || state.Facing == null)
        {
            return new StepResult(state, null);
        }

        // Calculate the new position based on the current direction
        var newPosition = state.Facing switch
        {
            Direction.North => new Position(state.Pos.X, state.Pos.Y + 1),
            Direction.East => new Position(state.Pos.X + 1, state.Pos.Y),
            Direction.South => new Position(state.Pos.X, state.Pos.Y - 1),
            Direction.West => new Position(state.Pos.X - 1, state.Pos.Y),
            _ => state.Pos
        };

        // Check if the new position is valid
        if (!_tabletop.IsValid(newPosition))
        {
            // Move would cause robot to fall - ignore and keep current state
            return new StepResult(state, null);
        }

        // Move is valid - update position
        var newState = state with { Pos = newPosition };
        return new StepResult(newState, null);
    }

    private StepResult ExecuteLeft(RobotState state)
    {
        // If robot is not placed, ignore the command
        if (!state.IsPlaced || state.Facing == null)
        {
            return new StepResult(state, null);
        }

        // Rotate 90 degrees counter-clockwise
        var newFacing = state.Facing switch
        {
            Direction.North => Direction.West,
            Direction.West => Direction.South,
            Direction.South => Direction.East,
            Direction.East => Direction.North,
            _ => state.Facing
        };

        var newState = state with { Facing = newFacing };
        return new StepResult(newState, null);
    }

    private StepResult ExecuteRight(RobotState state)
    {
        // If robot is not placed, ignore the command
        if (!state.IsPlaced || state.Facing == null)
        {
            return new StepResult(state, null);
        }

        // Rotate 90 degrees clockwise
        var newFacing = state.Facing switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => state.Facing
        };

        var newState = state with { Facing = newFacing };
        return new StepResult(newState, null);
    }

    private StepResult ExecuteReport(RobotState state)
    {
        // If robot is not placed, produce no output
        if (!state.IsPlaced || state.Pos == null || state.Facing == null)
        {
            return new StepResult(state, null);
        }

        // Format: X,Y,DIRECTION
        var output = $"{state.Pos.X},{state.Pos.Y},{state.Facing.Value.ToString().ToUpper()}";
        return new StepResult(state, output);
    }
}

