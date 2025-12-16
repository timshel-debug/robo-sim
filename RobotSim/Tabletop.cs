namespace RobotSim;

/// <summary>
/// Represents the tabletop with validation for positions.
/// </summary>
public class Tabletop
{
    /// <summary>
    /// Default width of the tabletop as specified in the requirements.
    /// </summary>
    public const int DefaultWidth = 5;
    
    /// <summary>
    /// Default height of the tabletop as specified in the requirements.
    /// </summary>
    public const int DefaultHeight = 5;

    /// <summary>
    /// Gets the width of the tabletop.
    /// </summary>
    public int Width { get; }
    
    /// <summary>
    /// Gets the height of the tabletop.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the Tabletop class.
    /// </summary>
    /// <param name="width">The width of the tabletop. Must be positive.</param>
    /// <param name="height">The height of the tabletop. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width or height is not positive.</exception>
    public Tabletop(int width = DefaultWidth, int height = DefaultHeight)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Checks if a position is within the valid bounds of the tabletop.
    /// </summary>
    /// <param name="position">The position to validate.</param>
    /// <returns>True if the position is within bounds; otherwise, false.</returns>
    public bool IsValid(Position position)
    {
        return position.X >= 0 && position.X < Width &&
               position.Y >= 0 && position.Y < Height;
    }
}

