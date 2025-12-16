using RobotSim;
using RobotSim.Commands;
using Xunit;

namespace RobotSim.Tests;

/// <summary>
/// Test Category B: Engine Tests (Unit) — Core Rules
/// Tests the execution logic and state transitions.
/// </summary>
public class EngineTests
{
    private readonly Tabletop _tabletop = new();
    private readonly Engine _engine;

    public EngineTests()
    {
        _engine = new Engine(_tabletop);
    }

    // Test Case B1: Commands before first valid PLACE are ignored
    [Fact]
    public void Execute_CommandsBeforePlacement_AreIgnored()
    {
        var state = RobotState.Initial();

        // Try MOVE before placement
        var result1 = _engine.Execute(state, new MoveCommand());
        Assert.False(result1.NewState.IsPlaced);
        Assert.Null(result1.ReportOutput);

        // Try LEFT before placement
        var result2 = _engine.Execute(result1.NewState, new LeftCommand());
        Assert.False(result2.NewState.IsPlaced);

        // Try REPORT before placement - should produce no output
        var result3 = _engine.Execute(result2.NewState, new ReportCommand());
        Assert.False(result3.NewState.IsPlaced);
        Assert.Null(result3.ReportOutput);
    }

    // Test Case B2: Invalid PLACE ignored (robot remains unplaced)
    [Theory]
    [InlineData(5, 5)]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    public void Execute_InvalidPlace_RobotRemainsUnplaced(int x, int y)
    {
        var state = RobotState.Initial();

        var result = _engine.Execute(state, new PlaceCommand(x, y, Direction.North));

        Assert.False(result.NewState.IsPlaced);
        Assert.Null(result.NewState.Pos);
    }

    // Test Case B3: Valid PLACE sets state correctly
    [Fact]
    public void Execute_ValidPlace_SetsStateCorrectly()
    {
        var state = RobotState.Initial();

        var result = _engine.Execute(state, new PlaceCommand(0, 0, Direction.North));

        Assert.True(result.NewState.IsPlaced);
        Assert.NotNull(result.NewState.Pos);
        Assert.Equal(0, result.NewState.Pos.X);
        Assert.Equal(0, result.NewState.Pos.Y);
        Assert.Equal(Direction.North, result.NewState.Facing);
    }

    // Test Case B4: PLACE can be re-issued
    [Fact]
    public void Execute_RePlaceCommand_OverwritesPositionAndFacing()
    {
        var state = RobotState.Initial();

        // First placement
        var result1 = _engine.Execute(state, new PlaceCommand(0, 0, Direction.North));
        Assert.True(result1.NewState.IsPlaced);

        // Second placement
        var result2 = _engine.Execute(result1.NewState, new PlaceCommand(4, 4, Direction.South));
        Assert.True(result2.NewState.IsPlaced);
        Assert.Equal(4, result2.NewState.Pos!.X);
        Assert.Equal(4, result2.NewState.Pos!.Y);
        Assert.Equal(Direction.South, result2.NewState.Facing);
    }

    // Test Case B5: LEFT rotation cycle
    [Fact]
    public void Execute_LeftCommand_RotatesCounterClockwise()
    {
        var state = new RobotState(new Position(0, 0), Direction.North);

        // NORTH -> WEST
        var result1 = _engine.Execute(state, new LeftCommand());
        Assert.Equal(Direction.West, result1.NewState.Facing);

        // WEST -> SOUTH
        var result2 = _engine.Execute(result1.NewState, new LeftCommand());
        Assert.Equal(Direction.South, result2.NewState.Facing);

        // SOUTH -> EAST
        var result3 = _engine.Execute(result2.NewState, new LeftCommand());
        Assert.Equal(Direction.East, result3.NewState.Facing);

        // EAST -> NORTH (full cycle)
        var result4 = _engine.Execute(result3.NewState, new LeftCommand());
        Assert.Equal(Direction.North, result4.NewState.Facing);
    }

    // Test Case B6: RIGHT rotation cycle
    [Fact]
    public void Execute_RightCommand_RotatesClockwise()
    {
        var state = new RobotState(new Position(0, 0), Direction.North);

        // NORTH -> EAST
        var result1 = _engine.Execute(state, new RightCommand());
        Assert.Equal(Direction.East, result1.NewState.Facing);

        // EAST -> SOUTH
        var result2 = _engine.Execute(result1.NewState, new RightCommand());
        Assert.Equal(Direction.South, result2.NewState.Facing);

        // SOUTH -> WEST
        var result3 = _engine.Execute(result2.NewState, new RightCommand());
        Assert.Equal(Direction.West, result3.NewState.Facing);

        // WEST -> NORTH (full cycle)
        var result4 = _engine.Execute(result3.NewState, new RightCommand());
        Assert.Equal(Direction.North, result4.NewState.Facing);
    }

    // Test Case B7: MOVE forward by direction
    [Theory]
    [InlineData(Direction.North, 2, 2, 2, 3)] // North increments Y
    [InlineData(Direction.South, 2, 2, 2, 1)] // South decrements Y
    [InlineData(Direction.East, 2, 2, 3, 2)]  // East increments X
    [InlineData(Direction.West, 2, 2, 1, 2)]  // West decrements X
    public void Execute_Move_MovesInCorrectDirection(Direction facing, int startX, int startY, int expectedX, int expectedY)
    {
        var state = new RobotState(new Position(startX, startY), facing);

        var result = _engine.Execute(state, new MoveCommand());

        Assert.Equal(expectedX, result.NewState.Pos!.X);
        Assert.Equal(expectedY, result.NewState.Pos!.Y);
    }

    // Test Case B8: MOVE blocked at edges (ignored, state unchanged)
    [Theory]
    [InlineData(0, 0, Direction.South)]  // Can't go south from (0,0)
    [InlineData(0, 0, Direction.West)]   // Can't go west from (0,0)
    [InlineData(4, 4, Direction.North)]  // Can't go north from (4,4)
    [InlineData(4, 4, Direction.East)]   // Can't go east from (4,4)
    [InlineData(0, 3, Direction.West)]   // Can't go west from (0,y)
    [InlineData(4, 3, Direction.East)]   // Can't go east from (4,y)
    [InlineData(3, 0, Direction.South)]  // Can't go south from (x,0)
    [InlineData(3, 4, Direction.North)]  // Can't go north from (x,4)
    public void Execute_MoveAtEdge_BlockedAndStateUnchanged(int x, int y, Direction facing)
    {
        var state = new RobotState(new Position(x, y), facing);

        var result = _engine.Execute(state, new MoveCommand());

        // Position should remain unchanged
        Assert.Equal(x, result.NewState.Pos!.X);
        Assert.Equal(y, result.NewState.Pos!.Y);
        Assert.Equal(facing, result.NewState.Facing);
    }

    // Test Case B9: REPORT output matches state
    [Fact]
    public void Execute_Report_OutputsCorrectFormat()
    {
        var state = new RobotState(new Position(0, 1), Direction.North);

        var result = _engine.Execute(state, new ReportCommand());

        Assert.Equal("0,1,NORTH", result.ReportOutput);
    }

    // Test Case B10: Example 1 from brief
    [Fact]
    public void Execute_Example1_ProducesExpectedOutput()
    {
        var state = RobotState.Initial();

        // PLACE 0,0,NORTH
        var result1 = _engine.Execute(state, new PlaceCommand(0, 0, Direction.North));
        
        // MOVE
        var result2 = _engine.Execute(result1.NewState, new MoveCommand());
        
        // REPORT
        var result3 = _engine.Execute(result2.NewState, new ReportCommand());

        Assert.Equal("0,1,NORTH", result3.ReportOutput);
    }

    // Test Case B11: Example 2 from brief
    [Fact]
    public void Execute_Example2_ProducesExpectedOutput()
    {
        var state = RobotState.Initial();

        // PLACE 0,0,NORTH
        var result1 = _engine.Execute(state, new PlaceCommand(0, 0, Direction.North));
        
        // LEFT
        var result2 = _engine.Execute(result1.NewState, new LeftCommand());
        
        // REPORT
        var result3 = _engine.Execute(result2.NewState, new ReportCommand());

        Assert.Equal("0,0,WEST", result3.ReportOutput);
    }

    // Additional test: Rotation doesn't change position
    [Fact]
    public void Execute_RotationCommands_DoNotChangePosition()
    {
        var state = new RobotState(new Position(2, 3), Direction.North);

        var result1 = _engine.Execute(state, new LeftCommand());
        Assert.Equal(2, result1.NewState.Pos!.X);
        Assert.Equal(3, result1.NewState.Pos!.Y);

        var result2 = _engine.Execute(result1.NewState, new RightCommand());
        Assert.Equal(2, result2.NewState.Pos!.X);
        Assert.Equal(3, result2.NewState.Pos!.Y);
    }

    // Additional test: Valid positions on all corners and edges
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 4)]
    [InlineData(4, 0)]
    [InlineData(4, 4)]
    [InlineData(2, 0)]
    [InlineData(2, 4)]
    [InlineData(0, 2)]
    [InlineData(4, 2)]
    public void Execute_PlaceOnValidPosition_Succeeds(int x, int y)
    {
        var state = RobotState.Initial();

        var result = _engine.Execute(state, new PlaceCommand(x, y, Direction.North));

        Assert.True(result.NewState.IsPlaced);
        Assert.Equal(x, result.NewState.Pos!.X);
        Assert.Equal(y, result.NewState.Pos!.Y);
    }

    // Additional test: Tabletop constructor validates inputs
    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    [InlineData(-1, 5)]
    [InlineData(5, -1)]
    [InlineData(0, 0)]
    public void Tabletop_InvalidDimensions_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tabletop(width, height));
    }

    // Additional test: Tabletop constructor accepts valid dimensions
    [Theory]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(1, 1)]
    [InlineData(3, 7)]
    public void Tabletop_ValidDimensions_CreatesSuccessfully(int width, int height)
    {
        var tabletop = new Tabletop(width, height);
        
        Assert.Equal(width, tabletop.Width);
        Assert.Equal(height, tabletop.Height);
    }

    // Additional test: REPORT output is culture-invariant
    [Fact]
    public void Execute_Report_IsCultureInvariant()
    {
        var state = new RobotState(new Position(1, 2), Direction.North);
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        
        try
        {
            // Test with Turkish culture (where 'I'.ToLower() != 'i')
            System.Globalization.CultureInfo.CurrentCulture = 
                System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            
            var result = _engine.Execute(state, new ReportCommand());
            
            // Should always output standard English direction names
            Assert.Equal("1,2,NORTH", result.ReportOutput);
            
            // Test all directions
            var testCases = new[]
            {
                (Direction.North, "NORTH"),
                (Direction.East, "EAST"),
                (Direction.South, "SOUTH"),
                (Direction.West, "WEST")
            };
            
            foreach (var (direction, expected) in testCases)
            {
                var testState = new RobotState(new Position(0, 0), direction);
                var testResult = _engine.Execute(testState, new ReportCommand());
                Assert.EndsWith(expected, testResult.ReportOutput);
            }
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    // Additional test: Engine Execute guards against null inputs
    [Fact]
    public void Execute_NullState_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            _engine.Execute(null!, new MoveCommand()));
    }

    [Fact]
    public void Execute_NullCommand_ThrowsArgumentNullException()
    {
        var state = RobotState.Initial();
        Assert.Throws<ArgumentNullException>(() => 
            _engine.Execute(state, null!));
    }

    // Additional test: IsPlaced is computed correctly
    [Fact]
    public void RobotState_IsPlaced_IsComputedProperty()
    {
        // Not placed when both are null
        var state1 = new RobotState(null, null);
        Assert.False(state1.IsPlaced);
        
        // Not placed when only position is set
        var state2 = new RobotState(new Position(0, 0), null);
        Assert.False(state2.IsPlaced);
        
        // Not placed when only direction is set
        var state3 = new RobotState(null, Direction.North);
        Assert.False(state3.IsPlaced);
        
        // Placed when both are set
        var state4 = new RobotState(new Position(0, 0), Direction.North);
        Assert.True(state4.IsPlaced);
    }
}

