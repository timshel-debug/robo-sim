namespace RobotSim.Commands;

/// <summary>
/// Base interface for all robot commands.
/// This is a marker interface used to implement the Command pattern,
/// allowing the Engine to process different command types polymorphically.
/// Concrete implementations include PlaceCommand, MoveCommand, LeftCommand, RightCommand, and ReportCommand.
/// </summary>
public interface ICommand
{
}

