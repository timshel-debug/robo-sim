namespace RobotSim.Commands;

/// <summary>
/// Command to place the robot at a specific position and direction.
/// </summary>
public record PlaceCommand(int X, int Y, Direction Facing) : ICommand;

