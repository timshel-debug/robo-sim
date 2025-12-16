using RobotSim;
using RobotSim.Commands;
using Xunit;

namespace RobotSim.Tests;

/// <summary>
/// Test Category A: Parser Tests (Unit)
/// Tests the parsing of command strings into ICommand objects.
/// </summary>
public class ParserTests
{
    private readonly Parser _parser = new();

    // Test Case A1: Parse PLACE command
    [Fact]
    public void TryParse_ValidPlaceCommand_ReturnsPlaceCommand()
    {
        var result = _parser.TryParse("PLACE 0,0,NORTH", out var command);

        Assert.True(result);
        Assert.NotNull(command);
        var placeCmd = Assert.IsType<PlaceCommand>(command);
        Assert.Equal(0, placeCmd.X);
        Assert.Equal(0, placeCmd.Y);
        Assert.Equal(Direction.North, placeCmd.Facing);
    }

    // Test Case A2: Parse PLACE with whitespace
    [Fact]
    public void TryParse_PlaceWithWhitespace_ReturnsPlaceCommand()
    {
        var result = _parser.TryParse("PLACE 1, 2, WEST", out var command);

        Assert.True(result);
        Assert.NotNull(command);
        var placeCmd = Assert.IsType<PlaceCommand>(command);
        Assert.Equal(1, placeCmd.X);
        Assert.Equal(2, placeCmd.Y);
        Assert.Equal(Direction.West, placeCmd.Facing);
    }

    // Test Case A3: Parse MOVE/LEFT/RIGHT/REPORT
    [Theory]
    [InlineData("MOVE", typeof(MoveCommand))]
    [InlineData("LEFT", typeof(LeftCommand))]
    [InlineData("RIGHT", typeof(RightCommand))]
    [InlineData("REPORT", typeof(ReportCommand))]
    public void TryParse_SimpleCommands_ReturnsCorrectCommand(string input, Type expectedType)
    {
        var result = _parser.TryParse(input, out var command);

        Assert.True(result);
        Assert.NotNull(command);
        Assert.IsType(expectedType, command);
    }

    // Test Case A4: Case-insensitivity
    [Theory]
    [InlineData("place 0,0,north")]
    [InlineData("Place 0,0,North")]
    [InlineData("PLACE 0,0,NORTH")]
    [InlineData("move")]
    [InlineData("MOVE")]
    [InlineData("Move")]
    public void TryParse_CaseInsensitive_ReturnsCommand(string input)
    {
        var result = _parser.TryParse(input, out var command);

        Assert.True(result);
        Assert.NotNull(command);
    }

    // Test Case A5: Invalid PLACE formats rejected
    [Theory]
    [InlineData("PLACE")]
    [InlineData("PLACE 1,2")]
    [InlineData("PLACE a,b,NORTH")]
    [InlineData("PLACE 1,2,NOPE")]
    [InlineData("PLACE 1,2,3,NORTH")]
    [InlineData("PLACE ,1,NORTH")]
    public void TryParse_InvalidPlaceFormats_ReturnsFalse(string input)
    {
        var result = _parser.TryParse(input, out var command);

        Assert.False(result);
        Assert.Null(command);
    }

    // Test Case A6: Unknown command rejected
    [Theory]
    [InlineData("JUMP")]
    [InlineData("FLY")]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_UnknownCommand_ReturnsFalse(string input)
    {
        var result = _parser.TryParse(input, out var command);

        Assert.False(result);
        Assert.Null(command);
    }

    // Additional test: All valid directions
    [Theory]
    [InlineData("NORTH", Direction.North)]
    [InlineData("EAST", Direction.East)]
    [InlineData("SOUTH", Direction.South)]
    [InlineData("WEST", Direction.West)]
    public void TryParse_AllDirections_ParsesCorrectly(string directionStr, Direction expected)
    {
        var result = _parser.TryParse($"PLACE 0,0,{directionStr}", out var command);

        Assert.True(result);
        var placeCmd = Assert.IsType<PlaceCommand>(command);
        Assert.Equal(expected, placeCmd.Facing);
    }
}

