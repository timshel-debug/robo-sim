namespace RobotSim;

/// <summary>
/// Represents a position on the tabletop with X and Y coordinates.
/// </summary>
/// <param name="X">The X coordinate (0 is the western edge).</param>
/// <param name="Y">The Y coordinate (0 is the southern edge).</param>
public record Position(int X, int Y);

