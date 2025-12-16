namespace RobotSim;

/// <summary>
/// Represents the current state of the robot.
/// </summary>
/// <param name="IsPlaced">Whether the robot has been placed on the tabletop</param>
/// <param name="Pos">The robot's current position (null if not placed)</param>
/// <param name="Facing">The direction the robot is facing (null if not placed)</param>
public record RobotState(bool IsPlaced, Position? Pos, Direction? Facing)
{
    /// <summary>
    /// Creates an initial state with the robot not placed on the tabletop.
    /// </summary>
    public static RobotState Initial() => new(false, null, null);
}

