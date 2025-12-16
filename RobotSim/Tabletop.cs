namespace RobotSim;

/// <summary>
/// Represents the tabletop with validation for positions.
/// </summary>
public class Tabletop
{
    public int Width { get; }
    public int Height { get; }

    public Tabletop(int width = 5, int height = 5)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Checks if a position is within the valid bounds of the tabletop.
    /// </summary>
    public bool IsValid(Position position)
    {
        return position.X >= 0 && position.X < Width &&
               position.Y >= 0 && position.Y < Height;
    }
}

