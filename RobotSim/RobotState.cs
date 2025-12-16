namespace RobotSim;

/// <summary>
/// Represents the current state of the robot.
/// The robot is considered placed when both position and facing direction are set.
/// </summary>
/// <param name="Pos">The robot's current position (null if not placed)</param>
/// <param name="Facing">The direction the robot is facing (null if not placed)</param>
public record RobotState(Position? Pos, Direction? Facing)
{
    /// <summary>
    /// Gets whether the robot has been placed on the tabletop.
    /// The robot is placed when both position and facing direction are non-null.
    /// </summary>
    public bool IsPlaced => Pos is not null && Facing is not null;
    
    /// <summary>
    /// Creates an initial state with the robot not placed on the tabletop.
    /// </summary>
    public static RobotState Initial() => new(null, null);
}

