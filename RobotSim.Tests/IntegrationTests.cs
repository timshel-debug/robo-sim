using RobotSim;
using RobotSim.Commands;
using Xunit;

namespace RobotSim.Tests;

/// <summary>
/// Test Category C: Integration Tests
/// Tests the end-to-end flow of parsing and executing commands.
/// </summary>
public class IntegrationTests
{
    // Test Case C1: End-to-end with array of commands
    [Fact]
    public void IntegrationTest_Example1_ProducesCorrectOutput()
    {
        var commands = new[]
        {
            "PLACE 0,0,NORTH",
            "MOVE",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Single(outputs);
        Assert.Equal("0,1,NORTH", outputs[0]);
    }

    [Fact]
    public void IntegrationTest_Example2_ProducesCorrectOutput()
    {
        var commands = new[]
        {
            "PLACE 0,0,NORTH",
            "LEFT",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Single(outputs);
        Assert.Equal("0,0,WEST", outputs[0]);
    }

    [Fact]
    public void IntegrationTest_IgnoredCommandsBeforePlacement()
    {
        var commands = new[]
        {
            "MOVE",
            "LEFT",
            "RIGHT",
            "REPORT",
            "PLACE 1,2,EAST",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        // Only the final REPORT should produce output
        Assert.Single(outputs);
        Assert.Equal("1,2,EAST", outputs[0]);
    }

    [Fact]
    public void IntegrationTest_BlockedMovesAtEdges()
    {
        var commands = new[]
        {
            "PLACE 0,0,SOUTH",
            "MOVE", // Blocked
            "REPORT",
            "LEFT", // Now facing EAST
            "MOVE", // Move to 1,0
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Equal(2, outputs.Count);
        Assert.Equal("0,0,SOUTH", outputs[0]);
        Assert.Equal("1,0,EAST", outputs[1]);
    }

    [Fact]
    public void IntegrationTest_RePlaceAndContinue()
    {
        var commands = new[]
        {
            "PLACE 1,2,NORTH",
            "MOVE",
            "REPORT",
            "PLACE 3,3,SOUTH", // Re-place
            "MOVE",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Equal(2, outputs.Count);
        Assert.Equal("1,3,NORTH", outputs[0]);
        Assert.Equal("3,2,SOUTH", outputs[1]);
    }

    [Fact]
    public void IntegrationTest_InvalidCommandsIgnored()
    {
        var commands = new[]
        {
            "INVALID",
            "PLACE 0,0,NORTH",
            "JUMP",
            "MOVE",
            "FLY",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Single(outputs);
        Assert.Equal("0,1,NORTH", outputs[0]);
    }

    [Fact]
    public void IntegrationTest_ComplexScenario()
    {
        var commands = new[]
        {
            "PLACE 1,1,NORTH",
            "MOVE",
            "MOVE",
            "LEFT",
            "MOVE",
            "REPORT",
            "RIGHT",
            "RIGHT",
            "MOVE",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Equal(2, outputs.Count);
        Assert.Equal("0,3,WEST", outputs[0]);
        Assert.Equal("1,3,EAST", outputs[1]);
    }

    [Fact]
    public void IntegrationTest_MultipleReports()
    {
        var commands = new[]
        {
            "PLACE 0,0,NORTH",
            "REPORT",
            "MOVE",
            "REPORT",
            "MOVE",
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Equal(3, outputs.Count);
        Assert.Equal("0,0,NORTH", outputs[0]);
        Assert.Equal("0,1,NORTH", outputs[1]);
        Assert.Equal("0,2,NORTH", outputs[2]);
    }

    [Fact]
    public void IntegrationTest_InvalidPlaceIgnored()
    {
        var commands = new[]
        {
            "PLACE 5,5,NORTH", // Invalid - out of bounds
            "MOVE", // Should be ignored (not placed yet)
            "PLACE 2,2,EAST", // Valid
            "REPORT"
        };

        var outputs = RunCommands(commands);

        Assert.Single(outputs);
        Assert.Equal("2,2,EAST", outputs[0]);
    }

    /// <summary>
    /// Helper method to run commands and collect report outputs.
    /// </summary>
    private List<string> RunCommands(string[] commandLines)
    {
        var tabletop = new Tabletop();
        var engine = new Engine(tabletop);
        var parser = new Parser();
        var state = RobotState.Initial();
        var outputs = new List<string>();

        foreach (var line in commandLines)
        {
            if (parser.TryParse(line, out var command) && command != null)
            {
                var result = engine.Execute(state, command);
                state = result.NewState;

                if (result.ReportOutput != null)
                {
                    outputs.Add(result.ReportOutput);
                }
            }
        }

        return outputs;
    }
}

