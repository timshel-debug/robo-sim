namespace RobotSim.Commands;

/// <summary>
/// Command to place the robot at a specific position and direction.
/// </summary>
/// <param name="X">The X coordinate where the robot should be placed.</param>
/// <param name="Y">The Y coordinate where the robot should be placed.</param>
/// <param name="Facing">The direction the robot should face after placement.</param>
public record PlaceCommand(int X, int Y, Direction Facing) : ICommand;

