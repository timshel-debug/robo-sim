# Toy Robot Simulator

A .NET C# console application that simulates a toy robot moving on a 5×5 tabletop.

## Description

This application simulates a toy robot that can be placed on a tabletop grid and moved around using simple commands. The robot can move forward, rotate left or right, and report its current position and direction. The application prevents the robot from falling off the table by ignoring any commands that would cause it to do so.

## Features

- **5×5 Tabletop**: The robot moves on a square grid with coordinates from (0,0) to (4,4)
- **Placement Gate**: The robot must be placed on the table with a valid `PLACE` command before other commands are accepted
- **Safe Movement**: Any command that would cause the robot to fall off the table is ignored
- **Re-placement**: The robot can be placed multiple times, overwriting its previous position
- **Case-insensitive**: Commands can be entered in any case (e.g., `PLACE`, `place`, or `Place`)

## Commands

| Command | Description |
|---------|-------------|
| `PLACE X,Y,F` | Place the robot at position (X,Y) facing direction F (NORTH, SOUTH, EAST, or WEST) |
| `MOVE` | Move the robot one unit forward in the direction it's currently facing |
| `LEFT` | Rotate the robot 90 degrees counter-clockwise |
| `RIGHT` | Rotate the robot 90 degrees clockwise |
| `REPORT` | Output the robot's current position and direction in the format "X,Y,F" |

## Requirements

- .NET SDK 8.0 or later

## Building the Application

```bash
dotnet build
```

## Running the Application

### With default commands file (commands.txt in current directory)

```bash
dotnet run --project RobotSim
```

### With a specific commands file

```bash
dotnet run --project RobotSim -- path/to/your/commands.txt
```

## Running Tests

The project includes comprehensive unit tests covering parsing, engine logic, and end-to-end integration scenarios.

```bash
dotnet test
```

### Test Coverage

- **Parser Tests**: 20+ tests covering command parsing, case-insensitivity, and invalid input handling
- **Engine Tests**: 40+ tests covering placement, movement, rotation, edge cases, and the examples from the specification
- **Integration Tests**: 10+ tests covering end-to-end scenarios with multiple commands

## Example Usage

### Example 1: Basic Movement

```
PLACE 0,0,NORTH
MOVE
REPORT
```

**Output**: `0,1,NORTH`

### Example 2: Rotation

```
PLACE 0,0,NORTH
LEFT
REPORT
```

**Output**: `0,0,WEST`

### Example 3: Prevented Fall

```
PLACE 0,0,SOUTH
MOVE
REPORT
```

**Output**: `0,0,SOUTH` (the MOVE command was ignored as it would cause the robot to fall)

## Project Structure

```
RobotSim/
├── Commands/           # Command types (ICommand, PlaceCommand, MoveCommand, etc.)
├── Direction.cs        # Enum for cardinal directions
├── Position.cs         # Record for 2D coordinates
├── RobotState.cs       # Record representing robot state
├── Tabletop.cs         # Tabletop validation logic
├── Engine.cs           # Core execution engine (deterministic, no I/O)
├── Parser.cs           # Command string parser
├── StepResult.cs       # Result type for command execution
└── Program.cs          # Entry point and file I/O

RobotSim.Tests/
├── ParserTests.cs      # Unit tests for command parsing
├── EngineTests.cs      # Unit tests for execution logic
└── IntegrationTests.cs # End-to-end integration tests
```

## Design Decisions

### Architecture

The application follows a clean separation of concerns:

- **Domain Models**: Simple, immutable types (records and enums) represent the core concepts
- **Parser**: Converts text commands into strongly-typed command objects
- **Engine**: Pure, deterministic logic that executes commands and produces new states
- **Runner**: Handles I/O, orchestrating the parser and engine

This design makes the core logic easily testable without any I/O dependencies.

### State Management

The robot's state is immutable - each command execution returns a new state rather than modifying the existing one. This makes the behavior predictable and easy to reason about.

### Error Handling

The application is robust to invalid input:
- Unknown commands are silently ignored
- Invalid PLACE commands (out of bounds or malformed) are ignored
- Commands that would cause the robot to fall are ignored
- The robot continues operating after ignored commands

## Assumptions

1. **Origin**: The origin (0,0) is at the **south-west corner** of the tabletop
2. **Direction Mapping**:
   - NORTH increases Y
   - SOUTH decreases Y
   - EAST increases X
   - WEST decreases X
3. **State Persistence**: The robot's state persists across all commands in a file
4. **Output**: Only REPORT commands produce output; all other commands execute silently

## Tradeoffs and Future Enhancements

The following features were **deliberately excluded** to keep the implementation within a 2-hour scope:

### Not Implemented (and why)

1. **Interactive REPL mode** (live stdin loop)
   - *Reason*: File-based input satisfies the requirements and is faster to test deterministically
   - *Implementation time*: ~15 minutes

2. **Configurable board size**
   - *Reason*: The specification fixes the board at 5×5; making it configurable adds complexity without clear benefit
   - *Implementation time*: ~10 minutes

3. **Structured logging / verbosity flags**
   - *Reason*: Not required by the specification; would add overhead
   - *Implementation time*: ~20 minutes

4. **Property-based testing / fuzzing**
   - *Reason*: While excellent for parser robustness, the setup time isn't justified for this scope
   - *Implementation time*: ~30 minutes

5. **Multiple robots / obstacles**
   - *Reason*: Explicitly out of scope ("no other obstructions")
   - *Implementation time*: ~45 minutes

6. **Comment support in commands.txt**
   - *Reason*: Not required; empty lines and unknown commands are already ignored
   - *Implementation time*: ~5 minutes

7. **Undo/Redo functionality**
   - *Reason*: Not required; would need command history tracking
   - *Implementation time*: ~25 minutes

### Next Steps

If this were to be extended for production use, consider:

- Adding comment support in command files (lines starting with `#`)
- Interactive mode for manual testing and demonstrations
- Visualization of the robot's position on the grid
- Command history and undo/redo
- Performance optimization for very large command files
- More detailed error messages (currently all errors are silent)

## Test Case Matrix

The test suite includes all required test cases from the specification:

### Parser Tests (Category A)
- ✅ A1: Parse PLACE command
- ✅ A2: Parse with whitespace variations
- ✅ A3: Parse MOVE/LEFT/RIGHT/REPORT
- ✅ A4: Case-insensitive parsing
- ✅ A5: Invalid PLACE formats rejected
- ✅ A6: Unknown commands rejected

### Engine Tests (Category B)
- ✅ B1: Commands before first valid PLACE are ignored
- ✅ B2: Invalid PLACE ignored (robot remains unplaced)
- ✅ B3: Valid PLACE sets state correctly
- ✅ B4: PLACE can be re-issued
- ✅ B5: LEFT rotation cycle (N→W→S→E→N)
- ✅ B6: RIGHT rotation cycle (N→E→S→W→N)
- ✅ B7: MOVE forward by direction
- ✅ B8: MOVE blocked at edges
- ✅ B9: REPORT output matches state
- ✅ B10: Example 1 from specification
- ✅ B11: Example 2 from specification

### Integration Tests (Category C)
- ✅ C1: End-to-end Example 1
- ✅ C2: End-to-end Example 2
- ✅ C3: Commands ignored before placement
- ✅ C4: Blocked moves at edges
- ✅ C5: Re-PLACE and continued operation
- ✅ C6: Invalid commands ignored
- ✅ C7: Complex multi-command scenarios

**Total Test Count**: 79 tests, all passing ✅

## License

This is a coding exercise implementation. Feel free to use and modify as needed.

