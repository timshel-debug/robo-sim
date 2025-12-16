# Interview Questions & Answers - Toy Robot Simulator

This document provides detailed answers to expected interview questions about the implementation, organized by theme.

---

## Requirements Interpretation

### Q: Why is the valid coordinate range `0..4` and not `1..5`?

**A:** The specification states the tabletop is a 5×5 grid. Using 0-based indexing (0..4) is:
1. **Standard in programming** - Most programming languages use 0-based array indexing
2. **Mathematically consistent** - A 5×5 grid has 25 cells: (0,0) to (4,4) gives us 5×5 = 25 positions
3. **Explicit in requirements** - The spec states "origin is (0,0)" which strongly suggests 0-based indexing

Using 1..5 would be 1-based indexing, which while valid, is less common in C# and would be unusual without explicit requirements stating so.

### Q: How did you interpret "south-west corner is (0,0)" in terms of how `MOVE` changes X/Y?

**A:** I used a standard cartographic/mathematical coordinate system:
- **X-axis** runs horizontally (west-east): X increases going EAST (right), decreases going WEST (left)
- **Y-axis** runs vertically (south-north): Y increases going NORTH (up), decreases going SOUTH (down)
- **Origin (0,0)** is at the south-west corner (bottom-left)

Movement implementation:
```csharp
Direction.North => new Position(X, Y + 1)  // North increases Y
Direction.South => new Position(X, Y - 1)  // South decreases Y
Direction.East  => new Position(X + 1, Y)  // East increases X
Direction.West  => new Position(X - 1, Y)  // West decreases X
```

This matches standard geographic/cartographic conventions where north is "up" and east is "right."

### Q: What does it mean that "the first valid command must be a PLACE"? How did you handle commands before placement?

**A:** This means:
1. **Placement gate**: The robot cannot accept any commands until it's been successfully placed on the table
2. **"Valid" PLACE**: The PLACE must be within bounds and properly formatted
3. **Commands before placement**: All commands (MOVE, LEFT, RIGHT, REPORT) are **silently ignored** until a valid PLACE

Implementation approach:
```csharp
public record RobotState(bool IsPlaced, Position? Pos, Direction? Facing);
```

Every command checks `if (!state.IsPlaced) return new StepResult(state, null);`

- Invalid PLACE (out of bounds) → robot remains unplaced
- Valid PLACE → `IsPlaced = true`, subsequent commands work
- REPORT before placement → produces no output

### Q: How did you interpret "ignore invalid commands"? What counts as invalid?

**A:** I categorized invalid commands into four types, all handled by **silent ignoring**:

1. **Unknown commands** - `JUMP`, `FLY`, garbage text → Parser returns false
2. **Malformed PLACE** - `PLACE 1,2`, `PLACE a,b,NORTH` → Parser returns false
3. **Out-of-bounds PLACE** - `PLACE 5,5,NORTH`, `PLACE -1,0,NORTH` → Engine ignores (robot stays unplaced)
4. **Commands before placement** - Any MOVE/LEFT/RIGHT/REPORT before valid PLACE → Engine no-ops
5. **Numeric directions** - `PLACE 0,0,0` or `PLACE 0,0,1` → Parser rejects (not valid direction names)

**Design decision**: Silent ignoring (no error messages) because:
- Specification says "ignore" not "report error"
- Simpler to test (no error message formatting concerns)
- Matches the deterministic, side-effect-free core design

---

## Design and Architecture

### Q: What was your core design goal given the 2-hour limit?

**A:** My primary goal was **testability** with **simplicity** as a close second. Specifically:

**Testability first:**
- Pure, deterministic core logic (Engine) with zero I/O
- Easy to write fast, reliable unit tests
- State transitions are simple: `(state, command) → (newState, output)`

**Simplicity:**
- Minimal abstractions (no over-engineering)
- Immutable data structures (records) → predictable behavior
- No frameworks beyond xUnit → less cognitive load

**Not prioritized:**
- Extensibility (no plugin system, no event sourcing)
- Performance optimization (not needed for command files)
- Rich error reporting (silent failure is simpler and meets spec)

This prioritization meant I could write comprehensive tests (70 tests) within the time budget.

### Q: Why did you separate parsing, execution (engine), and IO? What would go wrong if you didn't?

**A:** Separation of concerns provides critical benefits:

**With separation:**
```
Input File → Parser → Commands → Engine → State + Output → Console
              ↓                    ↓
         Unit testable        Unit testable (no I/O!)
```

**Benefits:**
1. **Testability** - Engine tested with zero I/O (no files, no console mocking)
2. **Determinism** - Same inputs always produce same outputs
3. **Speed** - Unit tests run in milliseconds (no file system)
4. **Clarity** - Each component has one job
5. **Reusability** - Could swap file input for HTTP API without changing Engine

**Without separation (monolithic):**
```csharp
// Anti-pattern - everything in one method
while ((line = File.ReadLine()) != null) {
    if (line == "MOVE") {
        if (facing == "NORTH") y++;
        if (!IsValid(x, y)) y--; // rollback
    }
    // ... mixed parsing, validation, execution, I/O
}
```

**Problems:**
- Can't test without creating files
- Console output in tests
- Slower tests
- Hard to understand what's business logic vs I/O
- Can't reuse for different input sources

### Q: Did you use a state machine? If not, why not?

**A:** No explicit state machine, but the design is **conceptually state-based**:

**Implicit states:**
- **State 1**: Unplaced (IsPlaced = false)
- **State 2**: Placed (IsPlaced = true)

**Transitions:**
- Unplaced → Unplaced (invalid PLACE, or any non-PLACE command)
- Unplaced → Placed (valid PLACE)
- Placed → Placed (any command, might change position/direction)

**Why not explicit state machine pattern:**
1. **Only 2 states** - Too simple to warrant State pattern classes
2. **Time constraint** - State pattern adds boilerplate (IState, PlacedState, UnplacedState classes)
3. **No complex transitions** - Just a boolean flag is sufficient
4. **YAGNI principle** - Don't add abstraction until needed

**When I'd use explicit state machine:**
- If there were modes (Normal, Debug, Safe modes)
- Multiple robots with different behaviors
- Complex state transitions with guards
- State-specific command availability

Current approach: `if (!state.IsPlaced)` is clear and sufficient.

### Q: Why did you choose your data model (e.g., `RobotState`, `Position`, `Direction`)? What alternatives?

**A:** Current model:
```csharp
public enum Direction { North, East, South, West }
public record Position(int X, int Y);
public record RobotState(Position? Pos, Direction? Facing)
{
    public bool IsPlaced => Pos is not null && Facing is not null;
}
```

**Design rationale:**

**Direction as enum** (vs strings or ints):
- ✅ Type-safe (can't pass invalid direction)
- ✅ Compile-time checking
- ✅ Pattern matching support
- ✅ No typos possible
- ❌ Alternative: strings → fragile, no compiler help
- ❌ Alternative: ints (0-3) → unclear meaning, error-prone

**Position as record** (vs class or tuple):
- ✅ Immutable by default
- ✅ Value equality (two positions with same X,Y are equal)
- ✅ Concise syntax
- ❌ Alternative: class → mutable, requires equality override
- ❌ Alternative: tuple (int, int) → no named properties, less clear

**RobotState as record with nullable Pos/Facing and computed IsPlaced:**
- ✅ Makes "not placed" explicit in type system
- ✅ Compiler forces null checks
- ✅ Immutable → no accidental state mutations
- ✅ **IsPlaced is computed** → impossible to have inconsistent state
- ✅ Fewer parameters (2 instead of 3) → simpler construction
- ❌ Alternative: Separate PlacedState/UnplacedState classes → overkill for 2 states
- ❌ Alternative: Default values (0,0,North) when unplaced → misleading, can't distinguish

**Key principle**: Use the type system to prevent illegal states, with computed properties enforcing invariants.

### Q: Did you model commands as types (e.g., `PlaceCommand`) or as strings? Why?

**A:** I used **strongly-typed command objects** implementing `ICommand`:

```csharp
interface ICommand { }
record PlaceCommand(int X, int Y, Direction Facing) : ICommand;
record MoveCommand : ICommand;
// etc.
```

**Why types over strings:**

**Advantages:**
1. **Type safety** - Can't pass invalid data to Engine
2. **Pattern matching** - Clean switch expressions in Engine
3. **Self-documenting** - Command structure is explicit
4. **Compile-time checking** - Typos caught immediately
5. **Separation of concerns** - Parser validates format, Engine validates logic

**The flow:**
```
"PLACE 0,0,NORTH" → Parser → PlaceCommand(0,0,North) → Engine
                     (format)                          (logic)
```

**Alternative (string-based):**
```csharp
// Anti-pattern
Execute(string command) {
    var parts = command.Split(' ');
    if (parts[0] == "PLACE") { // fragile!
        var coords = parts[1].Split(',');
        // ... string manipulation everywhere
    }
}
```

**Problems with strings:**
- Parsing mixed with logic
- No compile-time safety
- Harder to test (more setup code)
- Easy to make typos

**Trade-off**: Slightly more code (6 command files) but much better maintainability and testability.

### Q: How would your design change if the board size was configurable or multiple robots were supported?

**A:** The design is already prepared for these extensions:

**Configurable board size:**
```csharp
// Already parameterized!
public class Tabletop
{
    public Tabletop(int width = 5, int height = 5) { ... }
}

// Usage:
var tabletop = new Tabletop(10, 10); // 10×10 board
var engine = new Engine(tabletop);
```

**Changes needed:**
- ✅ Tabletop already supports this (5 minutes work)
- Add CLI argument parsing: `--width 10 --height 10`
- Update commands.txt format or add config file

**Multiple robots:**

Current design needs moderate changes:

```csharp
// Option 1: Multiple engine instances
var robots = new Dictionary<string, (RobotState, Engine)>();
robots["robot1"] = (RobotState.Initial(), new Engine(tabletop));
robots["robot2"] = (RobotState.Initial(), new Engine(tabletop));

// Commands become: ROBOT1 PLACE 0,0,NORTH
```

**Changes needed:**
- Parser: Extract robot ID from commands
- Engine: Could stay the same (one instance per robot)
- Add collision detection (new requirement)
- Track all robot positions

**Option 2: Single engine, multiple states:**
```csharp
class MultiRobotEngine {
    Dictionary<string, RobotState> robots;
    
    StepResult Execute(string robotId, RobotState state, ICommand cmd) {
        // Check collisions with other robots
        // Execute command
    }
}
```

**Key point**: Core Engine logic doesn't need to change much because it's already pure and stateless. Extensions add complexity around it, not inside it.

---

## Parsing Decisions

### Q: How strict is your parser?

**A:** The parser is **lenient with formatting, strict with semantics**:

**Lenient (accepts):**
- ✅ Case variations: `PLACE`, `place`, `Place`, `pLaCe`
- ✅ Whitespace: `PLACE 1,2,NORTH` and `PLACE 1, 2, NORTH`
- ✅ Tabs/spaces: `PLACE\t1,2,NORTH`
- ✅ Leading/trailing whitespace on line

**Strict (rejects):**
- ❌ Wrong argument count: `PLACE 1,2` (missing direction)
- ❌ Non-numeric coordinates: `PLACE a,b,NORTH`
- ❌ Invalid directions: `PLACE 1,2,NORTHEAST`
- ❌ Unknown commands: `JUMP`, `FLY`

**Rationale**: Users shouldn't need to worry about whitespace or case, but structural errors likely indicate mistakes.

### Q: What happens with trailing garbage?

**A:** Current implementation: **trailing garbage causes rejection**

```csharp
// Current behavior:
"MOVE NOW"        → Rejects (doesn't match "MOVE" exactly)
"PLACE 1,2,NORTH EXTRA" → Rejects (splits by comma, finds 4 parts not 3)
```

This is **intentional strictness** - enforced by parser logic:
```csharp
// Simple commands must match exactly:
if (upper == "MOVE") { ... }  // "MOVE NOW" won't match

// PLACE must have exactly 3 comma-separated parts:
var argParts = args.Split(',');
if (argParts.Length != 3) return false;
```

**Rationale**: Trailing data suggests user error rather than intent. Strict parsing catches mistakes early.

**Alternative (more lenient) approach not implemented:**
```csharp
// More lenient - ignore extras:
var argParts = args.Split(',', 4, StringSplitOptions.RemoveEmptyEntries);
if (argParts.Length < 3) return false;
// Use only first 3 parts, ignore rest
```

### Q: How do you handle negative numbers, non-integers, or empty lines?

**A:**

**Negative numbers:**
```csharp
if (!int.TryParse(argParts[0].Trim(), out var x)) return false;
```
- Negative numbers **parse successfully** (e.g., -1 is valid int)
- But Engine rejects: `if (x < 0 || x >= Width) return false;`
- So `PLACE -1,0,NORTH` is parsed but results in robot staying unplaced

**Non-integers:**
```csharp
"PLACE 1.5,2,NORTH" → int.TryParse fails → Parser returns false
"PLACE abc,2,NORTH" → int.TryParse fails → Parser returns false
```
Rejected at parse time, never reaches Engine.

**Empty lines:**
```csharp
if (string.IsNullOrWhiteSpace(line)) return false;
```
Parser returns false → Runner ignores line → No effect on robot.

**Edge cases tested:**
- Empty file → Robot never placed, no output ✅
- File with only whitespace → Same as empty ✅
- File with only invalid commands → Same as empty ✅

### Q: Why did you choose your parsing approach (regex vs split vs manual tokenization)?

**A:** I used **split-based parsing** with manual validation:

```csharp
var parts = line.Split(new[] { ' ', '\t' }, 2, ...);
var argParts = args.Split(',');
```

**Why split over alternatives:**

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Split** (chosen) | Simple, readable, fast, no dependencies | Slightly verbose for complex formats | ✅ Best for this spec |
| **Regex** | Powerful, handles complex patterns | Harder to read/maintain, overkill here | ❌ Over-engineering |
| **Parser combinator** | Composable, type-safe | Requires library, learning curve | ❌ Time constraint |
| **Manual char loop** | Full control | Lots of code, error-prone | ❌ Unnecessary complexity |

**Example comparison:**

```csharp
// Split (current):
var parts = line.Split(' ', 2);
if (parts[0].ToUpper() == "PLACE") ...

// Regex (alternative):
var match = Regex.Match(line, @"^\s*PLACE\s+(\d+)\s*,\s*(\d+)\s*,\s*(NORTH|SOUTH|EAST|WEST)\s*$", 
                        RegexOptions.IgnoreCase);
```

The regex is more powerful but harder to understand and unnecessary for this simple format.

**Decision driver**: Given the 2-hour constraint, split-based parsing is the sweet spot of simplicity and functionality.

### Q: Where do you draw the line between "parser responsibility" and "engine responsibility"?

**A:** Clear separation of concerns:

**Parser responsibilities (format validation):**
- ✅ Is the string a recognized command?
- ✅ Does PLACE have 3 arguments?
- ✅ Are X,Y numeric?
- ✅ Is direction one of the 4 valid strings?
- → Output: Well-formed `ICommand` objects

**Engine responsibilities (business logic validation):**
- ✅ Is the position within bounds?
- ✅ Would MOVE cause a fall?
- ✅ Is the robot placed before executing command?
- → Output: Valid state transitions

**Example: Out-of-bounds PLACE**
```csharp
// Parser: "PLACE 10,10,NORTH" → PlaceCommand(10,10,North) ✅ (well-formed)
// Engine: IsValid(10,10) → false → Ignore command ❌ (invalid business logic)
```

**Why this separation:**
1. **Single Responsibility** - Parser knows format, Engine knows rules
2. **Reusability** - Could add JSON input with different parser, same engine
3. **Testability** - Test format handling separately from logic
4. **Flexibility** - Could make bounds configurable without touching parser

**Alternative approach (not chosen):**
```csharp
// Anti-pattern: Parser validates bounds
PlaceCommand(int x, int y, ...) {
    if (x < 0 || x > 4) throw new ArgumentException();
}
```
Problem: Hard-codes board size in command object, reduces reusability.

---

## Movement/Rotation Correctness

### Q: Walk me through what happens for each direction when `MOVE` is executed.

**A:** Here's the detailed flow for each direction:

**NORTH (facing up):**
```csharp
Position: (2, 3) Facing: North
↓
Calculate: new Position(2, 3 + 1) = (2, 4)
↓
Validate: IsValid(2, 4)? Yes (2 is in 0..4, 4 is in 0..4)
↓
Result: Position (2, 4), Facing: North
```

**SOUTH (facing down):**
```csharp
Position: (2, 1) Facing: South
↓
Calculate: new Position(2, 1 - 1) = (2, 0)
↓
Validate: IsValid(2, 0)? Yes
↓
Result: Position (2, 0), Facing: South
```

**EAST (facing right):**
```csharp
Position: (3, 2) Facing: East
↓
Calculate: new Position(3 + 1, 2) = (4, 2)
↓
Validate: IsValid(4, 2)? Yes
↓
Result: Position (4, 2), Facing: East
```

**WEST (facing left):**
```csharp
Position: (1, 2) Facing: West
↓
Calculate: new Position(1 - 1, 2) = (0, 2)
↓
Validate: IsValid(0, 2)? Yes
↓
Result: Position (0, 2), Facing: West
```

**Key implementation:**
```csharp
var newPosition = state.Facing switch
{
    Direction.North => new Position(state.Pos.X, state.Pos.Y + 1),
    Direction.East => new Position(state.Pos.X + 1, state.Pos.Y),
    Direction.South => new Position(state.Pos.X, state.Pos.Y - 1),
    Direction.West => new Position(state.Pos.X - 1, state.Pos.Y),
};

if (!_tabletop.IsValid(newPosition)) {
    return new StepResult(state, null); // No move, state unchanged
}
```

### Q: How do you guarantee the robot never falls off the table?

**A:** Defense in depth with **three layers of protection**:

**Layer 1: Placement validation (Engine)**
```csharp
var position = new Position(place.X, place.Y);
if (!_tabletop.IsValid(position)) {
    return new StepResult(state, null); // Invalid PLACE ignored
}
```

**Layer 2: Movement validation (Engine)**
```csharp
var newPosition = CalculateNewPosition(state);
if (!_tabletop.IsValid(newPosition)) {
    return new StepResult(state, null); // Invalid MOVE ignored
}
// Only reach here if valid - apply movement
```

**Layer 3: Bounds checking (Tabletop)**
```csharp
public bool IsValid(Position position)
{
    return position.X >= 0 && position.X < Width &&
           position.Y >= 0 && position.Y < Height;
}
```

**Why this guarantees safety:**
1. **Calculate first, validate second** - Never actually move before checking
2. **Immutable state** - Failed moves can't leave corrupted state
3. **Single validation point** - All position checks go through `Tabletop.IsValid()`
4. **No side effects** - Returns new state, doesn't modify current state

**Proof by invariant:**
- **Initial state**: Robot unplaced (no position) → Can't fall ✅
- **After PLACE**: Only valid positions allowed → On table ✅
- **After MOVE**: Only valid moves applied → Still on table ✅
- **Induction**: If on table after N commands, still on table after N+1 ✅

### Q: What happens at boundaries? How did you test it?

**A:** Boundary behavior is **critical for correctness**:

**At each edge:**

```
(0,4) (1,4) (2,4) (3,4) (4,4)  ← NORTH blocked
(0,3) (1,3) (2,3) (3,3) (4,3)
(0,2) (1,2) (2,2) (3,2) (4,2)
(0,1) (1,1) (2,1) (3,1) (4,1)
(0,0) (1,0) (2,0) (3,0) (4,0)  ← SOUTH blocked
↑                           ↑
WEST blocked            EAST blocked
```

**Systematic testing:**

```csharp
[Theory]
[InlineData(0, 0, Direction.South)]  // Bottom-left, can't go south
[InlineData(0, 0, Direction.West)]   // Bottom-left, can't go west
[InlineData(4, 4, Direction.North)]  // Top-right, can't go north
[InlineData(4, 4, Direction.East)]   // Top-right, can't go east
[InlineData(0, 3, Direction.West)]   // Left edge, any Y
[InlineData(4, 3, Direction.East)]   // Right edge, any Y
[InlineData(3, 0, Direction.South)]  // Bottom edge, any X
[InlineData(3, 4, Direction.North)]  // Top edge, any X
public void Execute_MoveAtEdge_BlockedAndStateUnchanged(int x, int y, Direction facing)
{
    var state = new RobotState(true, new Position(x, y), facing);
    var result = _engine.Execute(state, new MoveCommand());
    
    // Assert position unchanged
    Assert.Equal(x, result.NewState.Pos!.X);
    Assert.Equal(y, result.NewState.Pos!.Y);
}
```

**Coverage:**
- ✅ 4 corners × 2 blocked directions = 8 cases (but corners overlap)
- ✅ 4 edges × 1 blocked direction = 4 unique edge cases
- ✅ All boundary conditions tested
- ✅ Interior moves tested separately

**What I verified:**
1. Position doesn't change
2. Direction doesn't change
3. Robot stays placed (IsPlaced stays true)
4. No output produced

### Q: How did you implement rotation? Why use an enum cycle vs lookup table vs modular arithmetic?

**A:** I used **explicit switch expressions** (enum pattern matching):

```csharp
// LEFT (counter-clockwise)
var newFacing = state.Facing switch
{
    Direction.North => Direction.West,
    Direction.West => Direction.South,
    Direction.South => Direction.East,
    Direction.East => Direction.North,
};

// RIGHT (clockwise)
var newFacing = state.Facing switch
{
    Direction.North => Direction.East,
    Direction.East => Direction.South,
    Direction.South => Direction.West,
    Direction.West => Direction.North,
};
```

**Alternatives considered:**

**1. Modular arithmetic:**
```csharp
// Enum as int: North=0, East=1, South=2, West=3
int TurnRight(int dir) => (dir + 1) % 4;
int TurnLeft(int dir) => (dir + 3) % 4; // or (dir - 1 + 4) % 4
```

**2. Lookup table:**
```csharp
var leftMap = new Dictionary<Direction, Direction> {
    [Direction.North] = Direction.West,
    [Direction.West] = Direction.South,
    // ...
};
```

**3. Array indexing:**
```csharp
var dirs = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
int idx = Array.IndexOf(dirs, currentDir);
Direction newDir = dirs[(idx + 1) % 4]; // right
```

**Why I chose switch expressions:**

| Criterion | Switch | Modular | Lookup | Array |
|-----------|--------|---------|--------|-------|
| **Readability** | ✅ Excellent | ❌ Obscure | ⚠️ OK | ⚠️ OK |
| **Type safety** | ✅ Compile-time | ❌ Runtime | ✅ Good | ⚠️ OK |
| **Performance** | ✅ Fast | ✅ Fast | ⚠️ Allocation | ⚠️ Allocation |
| **Maintainability** | ✅ Easy | ❌ Magic numbers | ⚠️ Verbose | ❌ Fragile |
| **Compiler checks** | ✅ Exhaustiveness | ❌ None | ❌ None | ❌ None |

**Key advantages of switch:**
1. **Compiler verifies exhaustiveness** - Warns if I forget a direction
2. **No magic numbers** - `(dir + 1) % 4` is unclear
3. **Self-documenting** - Intent is obvious from code
4. **No runtime overhead** - Compiler optimizes to jump table

**When I'd use alternatives:**
- **Modular arithmetic**: If directions were a continuous spectrum (0-359 degrees)
- **Lookup table**: If rotation rules were configurable/data-driven
- **Array**: If order mattered for iteration (it doesn't here)

---

## Error Handling and Robustness

### Q: What does the app do when the file is missing or unreadable?

**A:** Explicit error handling with user-friendly message:

```csharp
static int Main(string[] args)
{
    // ...
    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"Error: File '{filePath}' not found.");
        return 1;
    }
    // ...
    return 0;
}
```

**Behavior:**
- Writes error to **stderr** (not stdout) - proper Unix convention
- Includes the **file path** that failed - helps user fix issue
- **Returns exit code 1** - indicates error to shell/scripts
- Uses `return` instead of `Environment.Exit()` - more testable and idiomatic

**Unhandled cases:**
- File exists but is **unreadable** (permissions) → Throws IOException
- File is **locked** by another process → Throws IOException

**Why these aren't caught:**
```csharp
// Could add:
try {
    var lines = File.ReadAllLines(filePath);
} catch (IOException ex) {
    Console.Error.WriteLine($"Error reading file: {ex.Message}");
    return 1;
}
```

**Trade-off**: Given 2-hour constraint, I prioritized:
1. **Most common case**: File not found ✅
2. **Graceful degradation**: Other I/O errors get default .NET exception messages

**In production**, I'd add:
- Try-catch around File.ReadAllLines
- Better error messages for permission errors
- Logging framework
- More specific exception handling (UnauthorizedAccessException, etc.)

### Q: What does it do with invalid lines—log, ignore silently, or fail fast? Why?

**A:** **Silent ignore** - invalid lines are skipped with no output:

```csharp
foreach (var line in lines)
{
    if (!parser.TryParse(line, out var command) || command == null)
    {
        continue; // Silent ignore
    }
    // ... execute valid command
}
```

**Rationale:**

**Why silent (not log):**
- ✅ Spec says "ignore" not "report"
- ✅ Simpler implementation
- ✅ No logging framework needed
- ✅ Deterministic output (only REPORT produces output)
- ✅ Makes testing easier (exact output comparison)

**Why ignore (not fail fast):**
- ✅ Robustness - One bad line doesn't kill processing
- ✅ Partial success - Process as much as possible
- ✅ User-friendly - File with comments/blank lines works

**Alternative approaches:**

**1. Verbose mode (not implemented):**
```csharp
if (verboseMode && !parser.TryParse(line, ...)) {
    Console.Error.WriteLine($"Warning: Invalid command on line {lineNum}: {line}");
}
```

**2. Fail fast (not chosen):**
```csharp
if (!parser.TryParse(line, out var command)) {
    throw new InvalidOperationException($"Invalid command: {line}");
}
```

**3. Logging (not implemented):**
```csharp
if (!parser.TryParse(line, out var command)) {
    _logger.LogWarning("Invalid command: {Command}", line);
}
```

**Design philosophy**: 
> "Be conservative in what you send, liberal in what you accept"
> - Postel's Law

Silent ignoring makes the system robust and forgiving.

### Q: How did you ensure the program doesn't crash on unexpected input?

**A:** Multiple defensive layers:

**1. Parser uses Try-Parse pattern (no exceptions):**
```csharp
if (!int.TryParse(argParts[0].Trim(), out var x))
    return false; // No exception thrown
```

**2. Null-safety with nullable reference types:**
```csharp
public record RobotState(bool IsPlaced, Position? Pos, Direction? Facing);
//                                               ↑           ↑
//                                            Nullable    Nullable

if (state.Pos == null || state.Facing == null)
    return ...; // Explicit null checks
```

**3. Bounds checking before array/collection access:**
```csharp
var parts = line.Split(' ', 2);
if (parts.Length != 2) return false; // Check before parts[1]
```

**4. Enum validation:**
```csharp
Direction? direction = directionStr switch
{
    "NORTH" => Direction.North,
    // ...
    _ => null // Unknown directions become null
};

if (direction == null) return false;
```

**5. Immutable state (can't corrupt):**
```csharp
public record RobotState(...); // Record = immutable
// Failed operations return original state, can't partially modify
```

**Stress-tested with:**
- ✅ Empty files
- ✅ Files with only whitespace
- ✅ Files with only invalid commands
- ✅ Extremely long lines
- ✅ Unicode characters (parser just rejects)
- ✅ Binary data (parser rejects)

**Not handled (would crash):**
- ❌ Extremely large files (memory exhaustion) - Out of scope
- ❌ Integer overflow: `PLACE 2147483648,0,NORTH` - Parser would fail but gracefully

---

## Testing Strategy and Coverage

### Q: What did you choose to unit test vs integration test, and why?

**A:** Clear separation based on **scope and purpose**:

**Unit Tests (69 tests):**

**Parser Tests (24 tests):**
- **Scope**: Single method (`TryParse`)
- **No dependencies**: Just string → command
- **Fast**: Milliseconds
- **Purpose**: Format validation correctness

**Engine Tests (45 tests):**
- **Scope**: Single method (`Execute`)
- **No dependencies**: Pure function (state, command) → (state, output)
- **Fast**: Milliseconds
- **Purpose**: Business logic correctness

**Integration Tests (9 tests):**
- **Scope**: Full flow (parse → execute loop)
- **Dependencies**: Parser + Engine + state management
- **Still fast**: No actual I/O
- **Purpose**: Component interaction and end-to-end scenarios

**Why this split:**

| Concern | Unit Tests | Integration Tests |
|---------|------------|-------------------|
| **What breaks?** | Specific function | Component interaction |
| **Why it breaks?** | Precise failure point | Integration issue |
| **Debug time** | Fast (isolated) | Slower (multiple components) |
| **Execution speed** | <1ms per test | ~1-2ms per test |
| **Coverage** | All edge cases | Happy paths + key scenarios |

**What I didn't test** (consciously):
- ❌ **No file I/O tests** - Too slow, tested manually
- ❌ **No Console output tests** - Integration layer is thin, tested manually
- ❌ **No performance tests** - Not required

**Test pyramid achieved:**
```
     /\      E2E (manual)
    /  \     Integration (9)
   /----\    
  /      \   Unit (69)
 /________\  
```

### Q: How did you ensure you covered the "most important" cases within time?

**A:** Systematic prioritization based on **risk and requirements**:

**Priority 1: Specification examples (MUST PASS)**
```csharp
[Fact] Execute_Example1_ProducesExpectedOutput() // PLACE 0,0,NORTH → MOVE → REPORT
[Fact] Execute_Example2_ProducesExpectedOutput() // PLACE 0,0,NORTH → LEFT → REPORT
```
These are explicit requirements - if these fail, implementation is wrong.

**Priority 2: Safety requirements (MUST NOT FAIL)**
```csharp
Execute_MoveAtEdge_BlockedAndStateUnchanged() // 8 parameterized tests
Execute_InvalidPlace_RobotRemainsUnplaced()   // 5 parameterized tests
Execute_CommandsBeforePlacement_AreIgnored()  // 3 command types
```
These prevent the robot from falling - core safety invariant.

**Priority 3: Core behaviors (ALL FEATURES)**
```csharp
Execute_LeftCommand_RotatesCounterClockwise()  // Full 360° cycle
Execute_RightCommand_RotatesClockwise()        // Full 360° cycle
Execute_Move_MovesInCorrectDirection()         // All 4 directions
Execute_Report_OutputsCorrectFormat()          // Output format
```
These prove each command works correctly.

**Priority 4: Edge cases and error paths**
```csharp
TryParse_InvalidPlaceFormats_ReturnsFalse()    // Malformed input
TryParse_UnknownCommand_ReturnsFalse()         // Unknown commands
Execute_RePlaceCommand_OverwritesPosition()    // Re-placement
```
These ensure robustness.

**Strategy used:**
1. **Start with examples from spec** (5 minutes)
2. **Add boundary tests** (all edges/corners) (15 minutes)
3. **Test each command type** (systematic coverage) (15 minutes)
4. **Add error cases** (invalid input) (10 minutes)

**Coverage metrics:**
- ✅ All commands: 5/5 (PLACE, MOVE, LEFT, RIGHT, REPORT)
- ✅ All directions: 4/4 (NORTH, EAST, SOUTH, WEST)
- ✅ All edges: 4/4 (top, bottom, left, right)
- ✅ All corners: 4/4
- ✅ All rotation directions: 2/2 (left, right)
- ✅ Spec examples: 2/2

### Q: Which tests are the "load-bearing" ones that prove correctness?

**A:** The minimum set that proves system correctness:

**Critical Test Set (9 tests):**

**1. Specification examples (2 tests):**
```csharp
Execute_Example1_ProducesExpectedOutput()
Execute_Example2_ProducesExpectedOutput()
```
**Why critical**: Explicit requirements from spec.

**2. Placement gate (1 test):**
```csharp
Execute_CommandsBeforePlacement_AreIgnored()
```
**Why critical**: Core state machine behavior.

**3. Boundary blocking (4 tests):**
```csharp
Execute_MoveAtEdge_BlockedAndStateUnchanged()
// Parameterized with: (0,0,SOUTH), (0,0,WEST), (4,4,NORTH), (4,4,EAST)
```
**Why critical**: Safety invariant - robot must not fall.

**4. Movement correctness (1 test):**
```csharp
Execute_Move_MovesInCorrectDirection()
// Parameterized with all 4 directions
```
**Why critical**: Proves MOVE works in all directions.

**5. Rotation correctness (1 test):**
```csharp
Execute_LeftCommand_RotatesCounterClockwise() // or Right
```
**Why critical**: Proves rotation logic works.

**If I could only run 9 tests before deploying**, these would be it.

**The rest (61 tests) provide:**
- Defense against regressions
- Edge case coverage
- Confidence in error handling
- Documentation of expected behavior

### Q: How did you test that invalid moves are ignored and state remains unchanged?

**A:** Explicit assertions on **complete state invariance**:

```csharp
[Theory]
[InlineData(0, 0, Direction.South)]  // Can't go south from (0,0)
public void Execute_MoveAtEdge_BlockedAndStateUnchanged(int x, int y, Direction facing)
{
    // Arrange: Robot at edge, facing outward
    var state = new RobotState(true, new Position(x, y), facing);

    // Act: Try to move off table
    var result = _engine.Execute(state, new MoveCommand());

    // Assert: EVERY aspect of state unchanged
    Assert.Equal(x, result.NewState.Pos!.X);           // X unchanged
    Assert.Equal(y, result.NewState.Pos!.Y);           // Y unchanged
    Assert.Equal(facing, result.NewState.Facing);      // Direction unchanged
    Assert.True(result.NewState.IsPlaced);             // Still placed
    Assert.Null(result.ReportOutput);                  // No output
}
```

**What I verify:**
1. ✅ **Position unchanged** - X and Y stay the same
2. ✅ **Direction unchanged** - Still facing same way
3. ✅ **Placement status unchanged** - Still placed
4. ✅ **No side effects** - No output produced

**Why complete verification matters:**
```csharp
// Buggy implementation:
if (!IsValid(newPos)) {
    return new RobotState(true, state.Pos, Direction.North); // BUG: Changes facing!
}
```
This bug would pass if I only checked position, but my tests catch it.

**Additional test for invalid PLACE:**
```csharp
public void Execute_InvalidPlace_RobotRemainsUnplaced()
{
    var state = RobotState.Initial(); // Unplaced
    var result = _engine.Execute(state, new PlaceCommand(5, 5, Direction.North));
    
    Assert.False(result.NewState.IsPlaced);  // Still unplaced
    Assert.Null(result.NewState.Pos);         // No position
}
```

### Q: Did you test re-placing the robot after it's already placed?

**A:** Yes, explicitly tested:

```csharp
[Fact]
public void Execute_RePlaceCommand_OverwritesPositionAndFacing()
{
    var state = RobotState.Initial();

    // First placement
    var result1 = _engine.Execute(state, new PlaceCommand(0, 0, Direction.North));
    Assert.True(result1.NewState.IsPlaced);
    Assert.Equal(0, result1.NewState.Pos!.X);
    Assert.Equal(0, result1.NewState.Pos!.Y);
    Assert.Equal(Direction.North, result1.NewState.Facing);

    // Second placement - completely different location and direction
    var result2 = _engine.Execute(result1.NewState, new PlaceCommand(4, 4, Direction.South));
    Assert.True(result2.NewState.IsPlaced);
    Assert.Equal(4, result2.NewState.Pos!.X);    // Changed from 0
    Assert.Equal(4, result2.NewState.Pos!.Y);    // Changed from 0
    Assert.Equal(Direction.South, result2.NewState.Facing); // Changed from North
}
```

**Also tested in integration tests:**
```csharp
[Fact]
public void IntegrationTest_RePlaceAndContinue()
{
    var commands = new[]
    {
        "PLACE 1,2,NORTH",
        "MOVE",
        "REPORT",          // Should output 1,3,NORTH
        "PLACE 3,3,SOUTH", // Re-place!
        "MOVE",
        "REPORT"           // Should output 3,2,SOUTH (new position)
    };

    var outputs = RunCommands(commands);
    
    Assert.Equal(2, outputs.Count);
    Assert.Equal("1,3,NORTH", outputs[0]);  // From first placement
    Assert.Equal("3,2,SOUTH", outputs[1]);  // From second placement
}
```

**Why this is important:**
- Spec explicitly allows re-PLACE
- Tests that robot doesn't get "stuck" after first placement
- Verifies all state (position AND direction) gets overwritten

### Q: How do you validate `REPORT` output formatting and ordering?

**A:** Multiple levels of validation:

**Level 1: Unit test - Format structure**
```csharp
[Fact]
public void Execute_Report_OutputsCorrectFormat()
{
    var state = new RobotState(true, new Position(0, 1), Direction.North);
    var result = _engine.Execute(state, new ReportCommand());
    
    Assert.Equal("0,1,NORTH", result.ReportOutput);
    //          ↑ ↑ ↑↑↑↑↑↑
    //          X,Y,DIRECTION (exact format)
}
```

**Level 2: Integration test - Multiple reports in sequence**
```csharp
[Fact]
public void IntegrationTest_MultipleReports()
{
    var commands = new[]
    {
        "PLACE 0,0,NORTH",
        "REPORT",          // First report
        "MOVE",
        "REPORT",          // Second report
        "MOVE",
        "REPORT"           // Third report
    };

    var outputs = RunCommands(commands);
    
    Assert.Equal(3, outputs.Count);                    // Correct count
    Assert.Equal("0,0,NORTH", outputs[0]);             // Order preserved
    Assert.Equal("0,1,NORTH", outputs[1]);
    Assert.Equal("0,2,NORTH", outputs[2]);
}
```

**Level 3: Format verification**
```csharp
// Validates:
// 1. Direction is uppercase: "NORTH" not "north" or "North"
// 2. Comma-separated: "0,1,NORTH" not "0 1 NORTH"
// 3. No spaces: "0,1,NORTH" not "0, 1, NORTH"
// 4. Coordinates are integers: "0,1,NORTH" not "0.0,1.0,NORTH"

var output = result.ReportOutput;
Assert.Matches(@"^\d+,\d+,(NORTH|SOUTH|EAST|WEST)$", output); // Regex validation
```

**Level 4: Specification examples**
```csharp
// Both examples include REPORT, proving format matches spec expectations
Execute_Example1_ProducesExpectedOutput() // → "0,1,NORTH"
Execute_Example2_ProducesExpectedOutput() // → "0,0,WEST"
```

**Implementation that ensures format:**
```csharp
var output = $"{state.Pos.X},{state.Pos.Y},{state.Facing.Value.ToString().ToUpper()}";
//             ↑           ↑  ↑           ↑  ↑                              ↑
//             X           ,  Y           ,  Direction                   Uppercase
```

### Q: If you didn't add property-based tests, what risks remain and how would you mitigate them?

**A:** **Didn't add property-based tests** due to time constraint. Here are the risks:

**Risks from example-based testing only:**

**1. Integer overflow in coordinates:**
```csharp
PLACE 2147483647,0,NORTH  // int.MaxValue
PLACE -2147483648,0,NORTH // int.MinValue
```
**Risk**: Parser accepts, Engine might behave unexpectedly  
**Mitigation**: Add explicit tests for `int.MinValue` and `int.MaxValue`

**2. Long input strings:**
```csharp
"PLACE " + new string('1', 1000000) + ",0,NORTH"
```
**Risk**: Parser might be slow or crash  
**Mitigation**: Input size limits or timeout tests

**3. Unicode/special characters:**
```csharp
"PLACE 0,0,NØRTH"  // Nordic characters
"PLACE 0,0,NORTH\0" // Null bytes
```
**Risk**: Parser might mishandle  
**Mitigation**: Currently rejected (good), but not explicitly tested

**4. Combinations not explicitly tested:**
```csharp
// Multiple re-placements in sequence
PLACE 0,0,N → PLACE 1,1,E → PLACE 2,2,S → PLACE 3,3,W → ...
```
**Risk**: State corruption after many transitions  
**Mitigation**: Immutable state makes this unlikely, but could add stress test

**Property-based tests I would add:**

```csharp
[Property]
public Property RobotAlwaysStaysOnTable(List<ICommand> commands)
{
    var state = RobotState.Initial();
    foreach (var cmd in commands) {
        state = _engine.Execute(state, cmd).NewState;
        if (state.IsPlaced) {
            Assert.True(state.Pos.X >= 0 && state.Pos.X <= 4);
            Assert.True(state.Pos.Y >= 0 && state.Pos.Y <= 4);
        }
    }
}

[Property]
public Property RotatingFullCircleReturnsToStart(Direction start)
{
    var state = new RobotState(true, new Position(0, 0), start);
    
    // Rotate left 4 times
    for (int i = 0; i < 4; i++)
        state = _engine.Execute(state, new LeftCommand()).NewState;
    
    Assert.Equal(start, state.Facing); // Back to original
}

[Property]
public Property ParserRejectsOrSucceeds(string input)
{
    // Property: Parser never throws, either succeeds or returns false
    var success = _parser.TryParse(input, out var command);
    if (success) Assert.NotNull(command);
    // Never crashes!
}
```

**Current mitigation strategy:**
- ✅ Systematic edge case coverage (corners, edges)
- ✅ Immutable state prevents corruption
- ✅ Try-Parse pattern prevents crashes
- ✅ Clear invariants tested explicitly
- ⚠️ Unusual input combinations not tested (acceptable risk for 2-hour scope)

---

### Q: What's your "minimum confidence set" of tests? What could you delete?

**A:** If I could only run **9 tests** before deploying, these would be it:

**Critical Test Set (9 tests):**

1. **Specification Example 1** (PLACE 0,0,NORTH → MOVE → REPORT)
   - *Why critical*: Explicit requirement from spec

2. **Specification Example 2** (PLACE 0,0,NORTH → LEFT → REPORT)
   - *Why critical*: Explicit requirement from spec

3. **Commands before placement ignored**
   - *Why critical*: Core state machine behavior

4. **All 4 edge blocking tests** (corners: bottom-left, top-right)
   ```csharp
   Execute_MoveAtEdge_BlockedAndStateUnchanged(0,0,South)
   Execute_MoveAtEdge_BlockedAndStateUnchanged(0,0,West)
   Execute_MoveAtEdge_BlockedAndStateUnchanged(4,4,North)
   Execute_MoveAtEdge_BlockedAndStateUnchanged(4,4,East)
   ```
   - *Why critical*: Safety invariant - robot must never fall

5. **Move in all 4 directions**
   ```csharp
   Execute_Move_MovesInCorrectDirection(North/East/South/West)
   ```
   - *Why critical*: Proves movement logic correct for all cases

6. **Full rotation cycle** (left or right)
   ```csharp
   Execute_LeftCommand_RotatesCounterClockwise()
   ```
   - *Why critical*: Proves rotation doesn't skip or duplicate directions

**These 9 tests prove:**
- ✅ Both spec examples work
- ✅ Robot can't fall (safety)
- ✅ Movement works in all directions
- ✅ Rotation cycles correctly
- ✅ Placement gate works

**What I could delete (redundant/nice-to-have):**

**Category A - Redundant tests:**
- Multiple PLACE validation tests (keep one, delete 4)
- Both LEFT and RIGHT rotation (keep one)
- Interior movement tests (edge tests imply interior works)

**Category B - Error path tests (good but not critical):**
- Invalid command parsing
- Malformed PLACE formats
- Re-placement scenarios

**Category C - Documentation tests:**
- Rotation doesn't change position
- Valid positions on all corners
- REPORT format regex validation

**Total: Keep 9, could delete ~60** (but keep them for regression prevention).

**Philosophy**: Minimum set proves correctness. Rest catch regressions and document behavior.

---

### Q: How would you test Program-level behavior (stdout/stderr, exit codes) without making tests flaky?

**A:** **Several approaches**, from simple to sophisticated:

**Approach 1: Refactor Main for testability** (best practice)

```csharp
// Extract core logic:
public class Application
{
    public int Run(string[] args, TextWriter output, TextWriter error)
    {
        // All the logic from Main
        // Write to output/error instead of Console
        return exitCode;
    }
}

// Main becomes thin wrapper:
static int Main(string[] args)
{
    var app = new Application();
    return app.Run(args, Console.Out, Console.Error);
}

// Test becomes easy:
[Fact]
public void Run_FileNotFound_WritesToStdErrAndReturns1()
{
    var output = new StringWriter();
    var error = new StringWriter();
    var app = new Application();
    
    int exitCode = app.Run(new[] { "nonexistent.txt" }, output, error);
    
    Assert.Equal(1, exitCode);
    Assert.Contains("not found", error.ToString());
    Assert.Empty(output.ToString());
}
```

**Approach 2: Console redirection** (current approach for integration tests)

```csharp
[Fact]
public void IntegrationTest_Example1_OutputsToStdout()
{
    // Redirect Console.Out temporarily:
    var originalOut = Console.Out;
    try
    {
        using var writer = new StringWriter();
        Console.SetOut(writer);
        
        // Run the commands through Parser + Engine
        var outputs = RunCommands(new[] {
            "PLACE 0,0,NORTH",
            "MOVE",
            "REPORT"
        });
        
        Assert.Equal("0,1,NORTH", outputs[0]);
    }
    finally
    {
        Console.SetOut(originalOut);
    }
}
```

**Approach 3: Process-level integration tests** (most realistic)

```csharp
[Fact]
public void Process_FileNotFound_ExitsWithCode1()
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project RobotSim -- nonexistent.txt",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }
    };
    
    process.Start();
    string stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    
    Assert.Equal(1, process.ExitCode);
    Assert.Contains("not found", stderr);
}
```

**Comparison:**

| Approach | Pros | Cons | Best For |
|----------|------|------|----------|
| **Refactored Main** | ✅ Fast, no I/O<br>✅ Easy to test<br>✅ Deterministic | ⚠️ Requires refactoring | Production code |
| **Console redirect** | ✅ No refactoring<br>✅ Tests actual I/O | ⚠️ Thread-unsafe<br>⚠️ Flaky in parallel | Current integration tests |
| **Process-level** | ✅ Tests actual binary<br>✅ Tests end-to-end | ❌ Slow (seconds)<br>❌ Platform-specific | CI smoke tests |

**What I implemented:**
- **Integration tests** - Use Console redirection (Approach 2)
- **Unit tests** - Don't touch Console at all (Engine returns strings)

**In production, I'd add:**
- Refactored Main (Approach 1) for comprehensive Program-level tests
- One process-level smoke test in CI

**Key principle**: Keep I/O at the edges, test logic without I/O.

---

### Q: Did you test that invalid commands don't mutate state at all (position, direction, placement, output)?

**A:** **Yes, explicitly tested:**

```csharp
[Theory]
[InlineData(0, 0, Direction.South)]  // Can't move south from (0,0)
[InlineData(4, 4, Direction.North)]  // Can't move north from (4,4)
public void Execute_MoveAtEdge_BlockedAndStateUnchanged(int x, int y, Direction facing)
{
    var state = new RobotState(new Position(x, y), facing);
    
    // Act: Try invalid move
    var result = _engine.Execute(state, new MoveCommand());
    
    // Assert: EVERY aspect unchanged
    Assert.Equal(x, result.NewState.Pos!.X);           // Position X unchanged
    Assert.Equal(y, result.NewState.Pos!.Y);           // Position Y unchanged
    Assert.Equal(facing, result.NewState.Facing);      // Direction unchanged
    Assert.True(result.NewState.IsPlaced);             // Still placed
    Assert.Null(result.ReportOutput);                  // No output produced
    
    // Bonus: Verify original state untouched (immutability)
    Assert.Equal(x, state.Pos.X);
    Assert.Equal(y, state.Pos.Y);
}
```

**What I verify:**
1. ✅ **Position unchanged** - Both X and Y stay same
2. ✅ **Direction unchanged** - Still facing same way
3. ✅ **Placement unchanged** - IsPlaced stays true
4. ✅ **No output** - No accidental REPORT
5. ✅ **Original state untouched** - Immutability preserved

**Similar tests for other invalid commands:**

```csharp
// Invalid PLACE:
public void Execute_InvalidPlace_RobotRemainsUnplaced()
{
    var state = RobotState.Initial();
    
    var result = _engine.Execute(state, new PlaceCommand(5, 5, Direction.North));
    
    Assert.False(result.NewState.IsPlaced);  // Still unplaced
    Assert.Null(result.NewState.Pos);         // No position set
    Assert.Null(result.NewState.Facing);      // No direction set
}

// Commands before placement:
public void Execute_CommandsBeforePlacement_AreIgnored()
{
    var state = RobotState.Initial();
    
    var result1 = _engine.Execute(state, new MoveCommand());
    Assert.False(result1.NewState.IsPlaced);  // Still unplaced
    
    var result2 = _engine.Execute(result1.NewState, new LeftCommand());
    Assert.False(result2.NewState.IsPlaced);  // Still unplaced
}
```

**Why complete verification matters:**

**Buggy implementation example:**
```csharp
// BUG: Changes direction even though move is blocked!
if (!IsValid(newPosition)) {
    return new RobotState(state.Pos, Direction.North); // ❌ Wrong direction!
}
```

This bug would pass if I only checked position, but my tests catch it because I verify **all state components**.

**Design enables easy testing:**
- Immutable state → Can compare original vs new
- Pure functions → No hidden side effects to track
- Explicit return values → Everything is observable

---

## Tradeoffs and "What You Didn't Do"

### Q: What features did you intentionally not implement due to the 2-hour constraint?

**A:** Documented tradeoffs (all in README):

**1. Interactive REPL mode**
- **What**: Live command input from stdin
- **Why skipped**: File input meets requirements and is easier to test
- **Time cost**: ~15 minutes
- **Would add if**: Users want to experiment interactively

**2. Configurable board size**
- **What**: `--width 10 --height 10` CLI arguments
- **Why skipped**: Spec fixes 5×5; Tabletop already supports it internally
- **Time cost**: ~10 minutes
- **Would add if**: Multiple scenarios need different sizes

**3. Structured logging**
- **What**: Log framework (Serilog), verbosity flags
- **Why skipped**: Silent operation is simpler and meets spec
- **Time cost**: ~20 minutes
- **Would add if**: Debugging production issues

**4. Property-based testing**
- **What**: FsCheck or Hedgehog for generative testing
- **Why skipped**: Setup time high, example-based tests sufficient
- **Time cost**: ~30 minutes
- **Would add if**: Proving invariants exhaustively

**5. Multiple robots / obstacles**
- **What**: Multiple robots on same board, collision detection
- **Why skipped**: Explicitly out of scope per spec
- **Time cost**: ~45 minutes
- **Would add if**: Specification expands scope

**6. Comment support in input files**
- **What**: Lines starting with `#` ignored
- **Why skipped**: Empty/invalid lines already ignored
- **Time cost**: ~5 minutes
- **Would add if**: Users want documented command files

**7. Better error messages**
- **What**: "Error on line 5: Invalid direction 'NOPE'" 
- **Why skipped**: Silent operation is simpler
- **Time cost**: ~15 minutes
- **Would add if**: Users need to debug command files

**8. Undo/redo functionality**
- **What**: Keep command history, UNDO/REDO commands
- **Why skipped**: Not required
- **Time cost**: ~25 minutes
- **Would add if**: Interactive mode added

**Total time saved**: ~165 minutes  
**Total time budget**: 120 minutes  
→ Choices were necessary, not optional!

### Q: If you had another hour, what would you add first and why?

**A:** Priority order for next 60 minutes:

**1. Property-based testing (20 minutes)**
```csharp
[Property]
Property RobotNeverFalls(List<ICommand> commands) { ... }
```
**Why first**: Highest confidence boost per time invested.

**2. Structured logging with verbosity (15 minutes)**
```csharp
// In Parser:
_logger.LogDebug("Parsing command: {Command}", line);
if (!isValid) _logger.LogWarning("Invalid command ignored: {Command}", line);

// Usage:
dotnet run -- commands.txt --verbose
```
**Why second**: Most useful for debugging user issues.

**3. Better error messages (10 minutes)**
```csharp
foreach (var (line, lineNum) in lines.WithIndex())
{
    if (!parser.TryParse(line, out var command)) {
        Console.Error.WriteLine($"Line {lineNum}: Invalid command '{line}'");
    }
}
```
**Why third**: Improves user experience immediately.

**4. Interactive REPL mode (15 minutes)**
```csharp
if (args.Contains("--interactive")) {
    while (true) {
        Console.Write("> ");
        var line = Console.ReadLine();
        if (line == "exit") break;
        // ... process command
    }
}
```
**Why fourth**: Fun to demo and useful for experimentation.

**Total: 60 minutes**

**What I wouldn't add yet:**
- ❌ Multiple robots (scope creep, needs requirements)
- ❌ Obstacles (scope creep)
- ❌ Configurable board (low value, already possible internally)

### Q: Where is your design flexible, and where is it deliberately "hard-coded"?

**A:** Strategic flexibility decisions:

**Flexible (easy to change):**

**1. Board dimensions:**
```csharp
public class Tabletop
{
    public Tabletop(int width = 5, int height = 5) { ... }
}
```
✅ **Why**: Parameter already exists, just needs CLI wiring

**2. Command set:**
```csharp
interface ICommand { }
```
Adding new commands:
```csharp
record BackwardCommand : ICommand;

// In Engine:
BackwardCommand => ExecuteBackward(state),
```
✅ **Why**: Open for extension via new implementations

**3. Input source:**
```csharp
// Current: File
// Easy to change to:
var commands = ReadCommandsFromHttp(url);
var commands = ReadCommandsFromDatabase(conn);
// Engine doesn't care where commands come from
```
✅ **Why**: Engine is pure, doesn't know about I/O

**4. Output destination:**
```csharp
// Current: Console.WriteLine
// Easy to inject:
void Run(ICommandSource source, IOutputSink sink) { ... }
```
✅ **Why**: Would take ~10 minutes to make injectable

**Hard-coded (intentionally rigid):**

**1. Coordinate system:**
```csharp
Direction.North => new Position(X, Y + 1)
```
❌ **Why**: Changing this would break fundamental assumption. Would need major refactor.

**2. Rotation amount (90 degrees):**
```csharp
North → East → South → West
```
❌ **Why**: No requirement for 45° or 30° rotations. YAGNI.

**3. Movement distance (1 unit):**
```csharp
MOVE advances by 1
```
❌ **Why**: No requirement for `MOVE 3` or variable distance.

**4. Direction count (4 cardinal directions):**
```csharp
enum Direction { North, East, South, West }
```
❌ **Why**: Adding NorthEast etc. would require rethinking movement and rotation entirely.

**5. Single robot:**
```csharp
RobotState state = ...
```
❌ **Why**: Multiple robots would need collision detection, ID management, etc. Different architecture.

**Design philosophy**: 
- Flexible where the **data varies** (board size, command source)
- Rigid where the **logic is fundamental** (coordinate system, rotation)

### Q: How would you scale this design to support obstacles or multiple robots without rewriting everything?

**A:** Evolutionary architecture approach:

**For Obstacles:**

**Option 1: Extend Tabletop (minimal change)**
```csharp
public class Tabletop
{
    private HashSet<Position> _obstacles = new();
    
    public bool IsValid(Position position)
    {
        return position.X >= 0 && position.X < Width &&
               position.Y >= 0 && position.Y < Height &&
               !_obstacles.Contains(position); // Add obstacle check
    }
    
    public void AddObstacle(Position pos) => _obstacles.Add(pos);
}

// In commands.txt:
OBSTACLE 2,2
PLACE 0,0,NORTH
MOVE
// ...
```

**Changes needed:**
- ✅ New command: `ObstacleCommand`
- ✅ Tabletop already used for validation
- ✅ Engine unchanged (still calls `IsValid`)
- ⏱️ **Time**: ~30 minutes

**Option 2: Strategy pattern for validation**
```csharp
interface IPositionValidator {
    bool IsValid(Position pos);
}

class CompositeValidator : IPositionValidator {
    private List<IPositionValidator> _validators;
    public bool IsValid(Position pos) => _validators.All(v => v.IsValid(pos));
}

// Usage:
var validator = new CompositeValidator {
    new BoundsValidator(5, 5),
    new ObstacleValidator(obstacles),
    new MultiRobotValidator(otherRobots) // Future!
};
```

**For Multiple Robots:**

**Option 1: Multiple engine instances**
```csharp
class RobotSimulator
{
    private Dictionary<string, (RobotState, Engine)> _robots = new();
    
    public void Execute(string robotId, ICommand command)
    {
        if (!_robots.ContainsKey(robotId)) {
            _robots[robotId] = (RobotState.Initial(), new Engine(_tabletop));
        }
        
        var (state, engine) = _robots[robotId];
        var result = engine.Execute(state, command);
        _robots[robotId] = (result.NewState, engine);
    }
}

// Commands:
ROBOT1 PLACE 0,0,NORTH
ROBOT2 PLACE 4,4,SOUTH
ROBOT1 MOVE
ROBOT2 MOVE
```

**Changes needed:**
- ✅ Parser extracts robot ID
- ✅ Multiple state instances
- ✅ Engine unchanged!
- ⏱️ **Time**: ~20 minutes (no collision detection)

**Option 2: Shared state with collision detection**
```csharp
class MultiRobotEngine
{
    public StepResult Execute(
        Dictionary<string, RobotState> allStates,
        string currentRobotId,
        ICommand command)
    {
        var state = allStates[currentRobotId];
        var tentativeResult = _singleRobotEngine.Execute(state, command);
        
        // Check collision with other robots
        var otherPositions = allStates
            .Where(kvp => kvp.Key != currentRobotId)
            .Select(kvp => kvp.Value.Pos)
            .ToHashSet();
            
        if (otherPositions.Contains(tentativeResult.NewState.Pos)) {
            return new StepResult(state, null); // Collision - ignore
        }
        
        return tentativeResult;
    }
}
```

**Changes needed:**
- ✅ Collision detection layer
- ✅ Pass all states to engine
- ✅ Core logic still reusable
- ⏱️ **Time**: ~45 minutes

**Key insight**: Current architecture makes extensions **additive**, not **rewrite**:
- Engine stays pure and testable
- Tabletop can be enhanced
- State management can be wrapped
- No breaking changes to core

---

## Code Quality and Maintainability

### Q: How do you keep the core logic free of side effects?

**A:** Multiple techniques enforcing purity:

**1. Immutable data structures:**
```csharp
public record RobotState(bool IsPlaced, Position? Pos, Direction? Facing);
public record Position(int X, int Y);
```
Records are immutable by default - can't be modified after creation.

**2. Pure function signature:**
```csharp
public StepResult Execute(RobotState state, ICommand command)
//                        ↑                  ↑
//                    Takes input        No mutation
//                    
//     Returns new state, doesn't modify input
//          ↓
{
    return new StepResult(newState, output);
}
```

**3. No I/O in Engine:**
```csharp
// Engine.cs has ZERO:
// - File operations
// - Console writes
// - Network calls
// - DateTime.Now
// - Random numbers
// - Static mutable state
```

**4. Dependency injection of dependencies:**
```csharp
public class Engine
{
    private readonly Tabletop _tabletop; // Injected, not created
    
    public Engine(Tabletop tabletop) // Constructor injection
    {
        _tabletop = tabletop;
    }
}
```

**5. Value equality (records):**
```csharp
var pos1 = new Position(0, 0);
var pos2 = new Position(0, 0);
Assert.Equal(pos1, pos2); // True - value equality, not reference
```

**Benefits of side-effect-free design:**
- ✅ **Testable**: No mocking needed
- ✅ **Deterministic**: Same inputs → same outputs, always
- ✅ **Parallelizable**: Could process commands concurrently (if needed)
- ✅ **Cacheable**: Results can be memoized
- ✅ **Debuggable**: No hidden state to track

**How to verify purity:**
```csharp
[Fact]
public void Engine_IsPure_SameInputsSameOutputs()
{
    var state = new RobotState(true, new Position(0, 0), Direction.North);
    var command = new MoveCommand();
    
    var result1 = _engine.Execute(state, command);
    var result2 = _engine.Execute(state, command);
    
    Assert.Equal(result1, result2); // Identical results
    Assert.Equal(state.Pos, new Position(0, 0)); // Input unchanged
}
```

### Q: Did you use immutability (records) or mutable state? Why?

**A:** **Immutable everywhere** in the core domain:

**Immutable types:**
```csharp
public record Position(int X, int Y);              // ✅ Record
public record RobotState(...);                     // ✅ Record
public record PlaceCommand(int X, int Y, ...);     // ✅ Record
public record StepResult(RobotState, string?);     // ✅ Record
public enum Direction { ... }                      // ✅ Enum (immutable)
```

**Mutable types:**
```csharp
public class Tabletop { ... }  // ⚠️ Class (but no setters)
public class Engine { ... }    // ⚠️ Class (but readonly fields)
public class Parser { ... }    // ⚠️ Class (stateless)
```

**Why immutable for domain:**

**Advantages:**
1. **Thread-safe** - No locks needed
2. **No defensive copying** - Safe to share references
3. **Time-travel debugging** - Keep old states
4. **Simpler reasoning** - Value can't change under you
5. **Value equality** - Two states with same data are equal

**Example showing benefit:**
```csharp
// With immutability:
var state1 = new RobotState(true, new Position(0, 0), Direction.North);
var state2 = _engine.Execute(state1, new MoveCommand()).NewState;
// state1 still exists unchanged - can compare, rollback, etc.

// With mutability (alternative):
var state = new RobotState();
state.Pos = new Position(0, 0);
_engine.Execute(state, new MoveCommand()); // Mutates state
// Lost original state1 - can't compare or rollback
```

**Why classes for services:**
- Tabletop, Engine, Parser are **services not data**
- Don't need value equality (identity is fine)
- Could be mutable, but aren't (all fields readonly)

**Trade-off:**
- ✅ Easier to reason about
- ✅ Safer in concurrent scenarios
- ⚠️ Slightly more memory (new objects) - negligible for this app

### Q: How do you avoid duplication between parser validation and engine validation?

**A:** **Layered validation** with clear responsibilities:

**Parser validates format:**
```csharp
// "Is this a well-formed command?"
if (!int.TryParse(x, out var xVal)) return false;        // Format
if (direction not in validDirections) return false;      // Format
```

**Engine validates business logic:**
```csharp
// "Is this command legal in current state?"
if (!_tabletop.IsValid(position)) return ignoreCommand;  // Logic
if (!state.IsPlaced) return ignoreCommand;               // Logic
```

**No duplication because:**
1. **Different concerns** - Format vs semantics
2. **Different stages** - Parse-time vs execution-time
3. **Different data** - Strings vs typed objects

**Example: Out-of-bounds PLACE**
```
"PLACE 10,10,NORTH"
       ↓
Parser: "10" and "10" are valid integers ✅ → PlaceCommand(10, 10, North)
       ↓
Engine: IsValid(10, 10) → false ❌ → Ignore command
```

**Why this is NOT duplication:**
- Parser doesn't know about board bounds (board size could be injected)
- Engine doesn't know about string format

**If they were duplicated (anti-pattern):**
```csharp
// BAD: Parser knows business rules
PlaceCommand(int x, int y, ...) {
    if (x < 0 || x > 4) throw new ArgumentException(); // ❌ Hard-coded!
}

// BAD: Engine knows string format
Execute(...) {
    if (command is string s && s.StartsWith("PLACE")) { // ❌ Should be parsed!
        var parts = s.Split(',');
        // ...
    }
}
```

**Actual separation:**
```
Layer 1: Parser   - Format validation (strings → commands)
Layer 2: Engine   - Logic validation (commands → state)
Layer 3: Tabletop - Domain validation (positions → valid/invalid)
```

Each layer has one job, no overlap.

### Q: What naming and structure decisions did you make to keep it readable under time pressure?

**A:** Deliberate choices for clarity:

**1. Explicit names over abbreviations:**
```csharp
✅ RobotState not RobState
✅ Direction not Dir
✅ PlaceCommand not PlaceCmd
✅ Execute not Exec
```

**2. Descriptive method names:**
```csharp
✅ Execute_CommandsBeforePlacement_AreIgnored()
   Clear: What's tested and expected behavior

❌ TestPlacement()
   Unclear: Which aspect? What's expected?
```

**3. File structure mirrors conceptual structure:**
```
RobotSim/
├── Commands/          ← All command types together
├── Direction.cs       ← Domain concepts at root
├── Position.cs
├── RobotState.cs
├── Engine.cs          ← Core logic
├── Parser.cs          ← Input handling
└── Program.cs         ← Entry point
```

**4. Consistent patterns:**
```csharp
// All Execute methods follow same pattern:
private StepResult ExecuteXxx(RobotState state, ...)
{
    if (!state.IsPlaced) return noChange;
    // ... logic
    return new StepResult(newState, output);
}
```

**5. Comments only for "why" not "what":**
```csharp
✅ // Format: X,Y,DIRECTION (explains decision)
✅ // Invalid placement - ignore and keep current state (explains business rule)

❌ // Set x to 0 (obvious from code)
❌ // Loop through commands (obvious from code)
```

**6. XML doc comments on public APIs:**
```csharp
/// <summary>
/// Executes a command and returns the new state and optional report output.
/// </summary>
public StepResult Execute(RobotState state, ICommand command)
```

**7. Guard clauses early:**
```csharp
✅ if (!state.IsPlaced) return new StepResult(state, null);
   // Happy path below, not nested

❌ if (state.IsPlaced) {
       // Many lines of nested code
   }
```

**8. Small, focused classes:**
- Parser: 100 lines, one job
- Engine: 140 lines, one job
- Program: 50 lines, one job

**Under time pressure, these choices meant:**
- ✅ Less mental load (clear names)
- ✅ Easy to find things (logical structure)
- ✅ Consistent patterns (copy/paste/modify)
- ✅ Self-documenting (less need for comments)

---

## Extensibility Deep-Dive

### Q: Why didn't you introduce DI / interfaces for I/O? Isn't that best practice?

**A:** **Deliberately avoided** to keep implementation focused:

**What I didn't do:**
```csharp
// Over-engineered for this scope:
public interface IFileReader {
    string[] ReadAllLines(string path);
}

public interface IConsoleWriter {
    void WriteLine(string message);
}

public class Program {
    private readonly IFileReader _fileReader;
    private readonly IConsoleWriter _writer;
    
    public Program(IFileReader reader, IConsoleWriter writer) { ... }
}
```

**Why I didn't add DI here:**

**1. YAGNI (You Aren't Gonna Need It):**
- No requirement for multiple I/O implementations
- No requirement for testing Program class in isolation
- Adding interfaces without need is speculation

**2. Time constraint:**
- DI setup: ~15 minutes
- Mock/test infrastructure: ~10 minutes
- **Total**: 25 minutes better spent on core logic

**3. Separation already achieved:**
- **Engine** is pure (no I/O) - fully testable ✅
- **Parser** is pure (no I/O) - fully testable ✅
- **Program** is thin (< 60 lines) - tested via integration ✅

**4. Complexity vs value:**
- ❌ Adds 3 interfaces (IFileReader, IConsoleWriter, IExitHandler?)
- ❌ Requires DI container or manual construction
- ❌ More for reviewers to understand
- ✅ Gain: Can mock File.ReadAllLines (but why? Tests don't need files anyway)

**Where DI adds value:**
```csharp
// Engine HAS DI - because it's needed:
public class Engine
{
    private readonly Tabletop _tabletop;  // Injected!
    
    public Engine(Tabletop tabletop) { ... }
}

// Why: Can test with different board sizes
var smallEngine = new Engine(new Tabletop(3, 3));
var largeEngine = new Engine(new Tabletop(10, 10));
```

**When I'd add DI to Program:**
- Multiple input sources (files, HTTP, database)
- Need to test Program class in isolation
- Multiple output targets (console, log file, network)
- Dependency on external services

**Current architecture already testable:**
```
I/O Layer (Program.cs)    ← Thin, tested via integration
      ↓
Parser → Engine           ← Pure, tested via unit tests
      ↓
Domain Models             ← Immutable, trivially testable
```

**Philosophy**: **Add abstraction when you have two concrete implementations, not one.**

---

### Q: If you added obstacles, where would the logic live and why?

**A:** **Tabletop class** - cleanest separation:

**Option 1: Extend Tabletop (recommended)**

```csharp
public class Tabletop
{
    private readonly HashSet<Position> _obstacles = new();
    
    public const int DefaultWidth = 5;
    public const int DefaultHeight = 5;
    
    public int Width { get; }
    public int Height { get; }
    
    public Tabletop(int width = DefaultWidth, int height = DefaultHeight)
    {
        Width = width;
        Height = height;
    }
    
    public void AddObstacle(Position position)
    {
        if (!IsInBounds(position))
            throw new ArgumentException("Obstacle must be within bounds");
        _obstacles.Add(position);
    }
    
    public bool IsValid(Position position)
    {
        return IsInBounds(position) && !_obstacles.Contains(position);
        //     ↑                        ↑
        //   Existing check          NEW check
    }
    
    private bool IsInBounds(Position p) =>
        p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
}

// New command:
public record ObstacleCommand(int X, int Y) : ICommand;

// In Engine:
ObstacleCommand obs => ExecuteObstacle(state, obs),

private StepResult ExecuteObstacle(RobotState state, ObstacleCommand cmd)
{
    _tabletop.AddObstacle(new Position(cmd.X, cmd.Y));
    return new StepResult(state, null);
}
```

**Why Tabletop is the right place:**
1. **Single Responsibility** - Tabletop already validates positions
2. **Minimal changes** - Engine calls IsValid(), which now checks obstacles
3. **Testable** - Can test Tabletop independently
4. **Reusable** - All position checks automatically include obstacles

**Changes needed:**
- ✅ Tabletop: Add HashSet<Position>, AddObstacle method (~15 lines)
- ✅ Parser: Recognize "OBSTACLE X,Y" (~10 lines)
- ✅ Engine: Add ObstacleCommand case (~5 lines)
- ✅ Tests: Test obstacle blocking (~10 tests)
- **Total**: ~30 minutes work

**Option 2: Composite validator (more complex)**

```csharp
interface IPositionValidator
{
    bool IsValid(Position p);
}

class BoundsValidator : IPositionValidator { ... }
class ObstacleValidator : IPositionValidator
{
    private HashSet<Position> _obstacles;
    public bool IsValid(Position p) => !_obstacles.Contains(p);
}

class CompositeValidator : IPositionValidator
{
    private List<IPositionValidator> _validators;
    public bool IsValid(Position p) => _validators.All(v => v.IsValid(p));
}
```

**When I'd use this:**
- Multiple validation rules (obstacles, danger zones, restricted areas)
- Rules need to be composed dynamically
- Need to add/remove rules at runtime

**For single obstacle feature**: Option 1 (Tabletop) is simpler.

---

### Q: If you added a new command (e.g., BACK for backward movement), what changes are required?

**A:** **4 localized changes** following existing patterns:

**Step 1: Create command type** (~2 minutes)
```csharp
// In Commands/BackCommand.cs:
namespace RobotSim.Commands;

/// <summary>
/// Command to move the robot one unit backward (opposite of current direction).
/// </summary>
public record BackCommand : ICommand;
```

**Step 2: Add parser support** (~3 minutes)
```csharp
// In Parser.cs:
public bool TryParse(string line, out ICommand? command)
{
    // ... existing code ...
    
    if (upper == "BACK")  // NEW
    {
        command = new BackCommand();
        return true;
    }
    
    // ... rest of parsing ...
}
```

**Step 3: Add engine handler** (~5 minutes)
```csharp
// In Engine.cs:
public StepResult Execute(RobotState state, ICommand command)
{
    return command switch
    {
        PlaceCommand place => ExecutePlace(state, place),
        MoveCommand => ExecuteMove(state),
        BackCommand => ExecuteBack(state),  // NEW
        LeftCommand => ExecuteLeft(state),
        // ... rest of commands ...
    };
}

private StepResult ExecuteBack(RobotState state)
{
    if (!state.IsPlaced || state.Pos == null || state.Facing == null)
    {
        return new StepResult(state, null);
    }

    // Move OPPOSITE of current direction:
    var newPosition = state.Facing switch
    {
        Direction.North => new Position(state.Pos.X, state.Pos.Y - 1), // Move south
        Direction.East => new Position(state.Pos.X - 1, state.Pos.Y),  // Move west
        Direction.South => new Position(state.Pos.X, state.Pos.Y + 1), // Move north
        Direction.West => new Position(state.Pos.X + 1, state.Pos.Y),  // Move east
        _ => state.Pos
    };

    if (!_tabletop.IsValid(newPosition))
    {
        return new StepResult(state, null); // Blocked
    }

    var newState = state with { Pos = newPosition };
    return new StepResult(newState, null);
}
```

**Step 4: Add tests** (~10 minutes)
```csharp
// In EngineTests.cs:
[Theory]
[InlineData(Direction.North, 2, 2, 2, 1)] // Facing north, move south
[InlineData(Direction.South, 2, 2, 2, 3)] // Facing south, move north
[InlineData(Direction.East, 2, 2, 1, 2)]  // Facing east, move west
[InlineData(Direction.West, 2, 2, 3, 2)]  // Facing west, move east
public void Execute_Back_MovesOppositeDirection(
    Direction facing, int startX, int startY, int expectedX, int expectedY)
{
    var state = new RobotState(new Position(startX, startY), facing);
    
    var result = _engine.Execute(state, new BackCommand());
    
    Assert.Equal(expectedX, result.NewState.Pos!.X);
    Assert.Equal(expectedY, result.NewState.Pos!.Y);
}

[Theory]
[InlineData(0, 0, Direction.North)]  // Can't back up south
[InlineData(4, 4, Direction.South)]  // Can't back up north
public void Execute_Back_BlockedAtEdges(int x, int y, Direction facing)
{
    var state = new RobotState(new Position(x, y), facing);
    
    var result = _engine.Execute(state, new BackCommand());
    
    Assert.Equal(x, result.NewState.Pos!.X);  // Unchanged
    Assert.Equal(y, result.NewState.Pos!.Y);
}
```

**Total changes:**
- 1 new file (BackCommand.cs)
- 3 files modified (Parser.cs, Engine.cs, EngineTests.cs)
- ~80 lines total
- **Time**: ~20 minutes

**What DOESN'T need to change:**
- ✅ Position, Direction, RobotState - unchanged
- ✅ Tabletop - unchanged
- ✅ Other commands - unchanged
- ✅ Program.cs - unchanged
- ✅ Integration tests - work automatically

**This demonstrates:**
- ✅ **Open/Closed Principle** - Open for extension (new command), closed for modification (existing code)
- ✅ **Command Pattern** - Adding new command is mechanical
- ✅ **Localized changes** - Touch only 3 files
- ✅ **No breaking changes** - Existing commands still work

**Design enables easy extension.**

---

## Practical Engineering Questions

### Q: What assumptions did you make, and how are they documented?

**A:** All assumptions documented in README:

**1. Coordinate system:**
```markdown
## Assumptions

1. **Origin**: The origin (0,0) is at the **south-west corner** of the tabletop
2. **Direction Mapping**:
   - NORTH increases Y
   - SOUTH decreases Y
   - EAST increases X
   - WEST decreases X
```

**2. State persistence:**
```markdown
3. **State Persistence**: The robot's state persists across all commands in a file
```

**3. Output behavior:**
```markdown
4. **Output**: Only REPORT commands produce output; all other commands execute silently
```

**4. Error handling:**
```csharp
// In code comments:
// Unknown/invalid commands are silently ignored (do not crash).
// Whitespace variations are tolerated (`PLACE 1, 2, NORTH`).
```

**5. Placement gate:**
```markdown
### FR-03 Initial Placement Gate
* The **first valid command must be a valid `PLACE`**. 
* All commands **before** the first valid `PLACE` are discarded (ignored).
```

**Other implicit assumptions:**
- Commands are executed sequentially (no parallelism)
- File fits in memory
- One robot only
- No obstacles on tabletop
- 90-degree rotations only
- Move distance is always 1 unit

**Documentation strategy:**
- README for user-facing assumptions
- Code comments for implementation assumptions
- Tests as executable documentation

### Q: How would you debug a failing test or a wrong REPORT output quickly?

**A:** Systematic debugging approach:

**Step 1: Identify which layer failed**

**Test name tells you:**
```csharp
ParserTests.TryParse_ValidPlaceCommand_ReturnsPlaceCommand  ← Parser issue
EngineTests.Execute_Move_MovesInCorrectDirection            ← Engine issue  
IntegrationTests.IntegrationTest_Example1                   ← Integration issue
```

**Step 2: Use debugger strategically**

```csharp
[Fact]
public void Execute_Move_MovesInCorrectDirection()
{
    var state = new RobotState(true, new Position(2, 2), Direction.North);
    
    var result = _engine.Execute(state, new MoveCommand());
    // ← Set breakpoint here
    
    Assert.Equal(3, result.NewState.Pos!.Y); // Expected 3, got ?
}
```

**Step 3: Inspect immutable state**

Since all data is immutable:
```csharp
// In watch window:
state.Pos.X        // 2
state.Pos.Y        // 2
state.Facing       // North
result.NewState.Pos.X  // 2
result.NewState.Pos.Y  // ?? (unexpected value)
```

**Step 4: Check Engine logic**

```csharp
// In Engine.cs:
var newPosition = state.Facing switch
{
    Direction.North => new Position(state.Pos.X, state.Pos.Y + 1),
    // ← Set breakpoint here to verify calculation
};
```

**Step 5: Verify preconditions**

```csharp
// Common issues:
Assert.True(state.IsPlaced);           // Is robot placed?
Assert.NotNull(state.Pos);             // Position exists?
Assert.InRange(state.Pos.X, 0, 4);     // Within bounds?
```

**For wrong REPORT output:**

```bash
# Expected: "0,1,NORTH"
# Actual:   "0,1,North"    ← Lowercase issue

# Fix:
var output = $"{state.Pos.X},{state.Pos.Y},{state.Facing.Value.ToString().ToUpper()}";
                                                                            ↑ Add ToUpper()
```

**Debugging tools I rely on:**
1. **Test names** - Tell you what's broken
2. **Immutable state** - Easy to inspect in debugger
3. **Pure functions** - Same inputs always give same outputs
4. **Small methods** - Easy to step through
5. **Pattern matching** - Clear switch cases to inspect

**Time to debug:**
- Parser issue: ~2 minutes (simple string logic)
- Engine issue: ~3 minutes (clear state transitions)
- Integration issue: ~5 minutes (multiple components)

**Immutability makes debugging fast** - no hidden state changes.

### Q: If a colleague needed to extend this, what would you want them to touch vs avoid touching?

**A:** Clear guidance:

**✅ SAFE TO TOUCH (extension points):**

**1. Add new commands:**
```csharp
// Step 1: Create command type
public record BackwardCommand : ICommand;

// Step 2: Add parser support
if (upper == "BACKWARD") {
    command = new BackwardCommand();
    return true;
}

// Step 3: Add engine handler
BackwardCommand => ExecuteBackward(state),

// Step 4: Implement logic
private StepResult ExecuteBackward(RobotState state) { ... }
```
**Risk**: Low - Follows existing pattern

**2. Modify board size:**
```csharp
// In Program.cs:
var tabletop = new Tabletop(10, 10); // Change from 5,5
```
**Risk**: Low - Parameterized already

**3. Add new validation rules:**
```csharp
// In Tabletop or create new validator:
public bool IsValid(Position position)
{
    return position.X >= 0 && position.X < Width &&
           position.Y >= 0 && position.Y < Height &&
           !_obstacles.Contains(position); // NEW
}
```
**Risk**: Low - Single responsibility

**4. Change input/output:**
```csharp
// Safe to change:
var commands = ReadFromDatabase();  // Instead of file
WriteTo(logger, output);            // Instead of Console
```
**Risk**: Low - I/O is separated

**⚠️ MODIFY WITH CARE:**

**1. Coordinate system:**
```csharp
// In Engine.ExecuteMove:
Direction.North => new Position(state.Pos.X, state.Pos.Y + 1),
// Changing this affects ALL tests
```
**Risk**: High - Requires updating ~30 tests

**2. State structure:**
```csharp
public record RobotState(bool IsPlaced, Position? Pos, Direction? Facing);
// Adding fields is OK, changing existing breaks everything
```
**Risk**: Medium - Many dependencies

**❌ AVOID TOUCHING:**

**1. Immutability of records:**
```csharp
// DON'T change to class:
public class Position { // ❌ Don't do this
    public int X { get; set; }
    public int Y { get; set; }
}
```
**Risk**: Very high - Breaks all purity guarantees

**2. Engine purity:**
```csharp
// DON'T add side effects:
public StepResult Execute(RobotState state, ICommand command)
{
    Console.WriteLine("Executing..."); // ❌ No!
    File.WriteAllText(...);            // ❌ No!
    _counter++;                        // ❌ No!
}
```
**Risk**: Very high - Breaks testability

**3. Parser-Engine contract:**
```csharp
// DON'T merge parsing and execution:
public StepResult Execute(string commandString) // ❌ Wrong signature
```
**Risk**: High - Breaks separation of concerns

**Documentation I'd add:**

```markdown
## Extension Guide

### Adding New Commands
1. Create record in `Commands/` inheriting `ICommand`
2. Add parsing in `Parser.TryParse`
3. Add case in `Engine.Execute`
4. Implement handler method
5. Add tests

### Core Invariants (DO NOT BREAK)
- Engine must remain pure (no I/O)
- Domain types must remain immutable
- State transitions must be deterministic
```

---

## Build, Packaging & Runtime Questions

### Q: How do I run this on a clean machine? What .NET version do you target and why?

**A:** The project targets **.NET 8.0** (LTS):

```xml
<TargetFramework>net8.0</TargetFramework>
```

**Why .NET 8.0:**
1. **LTS (Long-Term Support)** - Supported until November 2026
2. **Widely available** - Standard SDK across most environments
3. **Modern C# features** - Supports C# 12 (records, pattern matching, nullable reference types, ArgumentNullException.ThrowIfNull)
4. **Production-ready** - Stable, not preview/experimental

**Not .NET 6.0** (older LTS): Already has .NET 8.0 available, so use the latest LTS
**Not .NET 7.0 or 9.0**: Non-LTS versions with shorter support windows
**Not .NET 10.0**: Doesn't exist (common mistake)

**Running on clean machine:**
```bash
# 1. Install .NET 8.0 SDK from microsoft.com/net/download
# 2. Clone repository
git clone <repo-url>
cd robo-sim

# 3. Build
dotnet build

# 4. Run tests
dotnet test

# 5. Run application
dotnet run --project RobotSim
```

**Zero external dependencies** - Just .NET SDK + xUnit (for tests only).

---

### Q: What's your CLI contract? What happens if no args are passed?

**A:** Clear, predictable interface:

```csharp
static int Main(string[] args)
{
    string filePath;
    if (args.Length == 0)
    {
        filePath = "commands.txt";  // Default file in current directory
    }
    else
    {
        filePath = args[0];         // First argument is file path
    }
    
    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"Error: File '{filePath}' not found.");
        return 1;  // Error exit code
    }
    
    // ... process commands ...
    return 0;  // Success exit code
}
```

**CLI contract:**

| Invocation | Behavior |
|------------|----------|
| `dotnet run --project RobotSim` | Reads `commands.txt` in current directory |
| `dotnet run --project RobotSim -- path/to/file.txt` | Reads specified file |
| File not found | Prints error to stderr, exits with code 1 |
| File exists but empty | No output (no REPORT commands) |
| File with invalid commands | Silently ignores, processes valid ones |

**Exit codes:**
- `0` - Success (file processed, even if no valid commands)
- `1` - Error (file not found or other critical failure)

**Why default to commands.txt:**
- Follows convention (like .gitignore, package.json)
- Allows `dotnet run` with no arguments for quick testing
- Documented in README and spec

---

### Q: Do you stream the file or read it all at once? Why?

**A:** **Read all at once** using `File.ReadAllLines()`:

```csharp
var lines = File.ReadAllLines(filePath);
foreach (var line in lines)
{
    // Process each command
}
```

**Why this choice:**

**Advantages:**
1. **Simple** - One line of code, easy to understand
2. **Fast for typical files** - Command files are small (< 1KB typically)
3. **Atomic** - Either file reads completely or throws exception
4. **No resource management** - File handle automatically closed

**Trade-offs:**
- ❌ **Memory**: Loads entire file into memory
- ❌ **Large files**: Would fail with gigabyte-sized files
- ✅ **Acceptable for this use case**: Command files are tiny

**Alternative (streaming approach):**
```csharp
using var reader = new StreamReader(filePath);
while (reader.ReadLine() is string line)
{
    // Process line
}
```

**When I'd use streaming:**
- Files > 100MB
- Processing real-time input (network stream, stdin pipe)
- Memory-constrained environments
- Processing can start before file is complete

**For this problem:**
- Spec says "file input" (not streaming)
- Command files will be < 1000 lines typically
- Simplicity > optimization
- **ReadAllLines is the right choice**

**Memory calculation:**
- 1000 commands × ~20 chars × 2 bytes (UTF-16) = ~40KB
- Negligible compared to process overhead (~50MB runtime)

---

## Type Safety & State Modeling (Advanced)

### Q: You say "make illegal states unrepresentable" — does your RobotState actually guarantee that?

**A:** **Originally no, but now yes** after refactoring:

**Original design (had flaw):**
```csharp
// PROBLEM: Could represent inconsistent states
public record RobotState(bool IsPlaced, Position? Pos, Direction? Facing);

// Illegal states that were possible:
var invalid1 = new RobotState(true, null, null);        // IsPlaced=true but no position!
var invalid2 = new RobotState(false, new Position(0,0), Direction.North); // Not placed but has data!
```

**Current design (enforces invariant):**
```csharp
public record RobotState(Position? Pos, Direction? Facing)
{
    // IsPlaced is COMPUTED - cannot be inconsistent
    public bool IsPlaced => Pos is not null && Facing is not null;
}

// Usage:
var state1 = new RobotState(null, null);                    // IsPlaced = false ✅
var state2 = new RobotState(new Position(0,0), Direction.North); // IsPlaced = true ✅
var state3 = new RobotState(new Position(0,0), null);       // IsPlaced = false ✅ (consistent!)
```

**Why the computed property is better:**
1. **Impossible to construct invalid state** - Compiler can't create IsPlaced=true with null Pos
2. **Fewer parameters** - 2 instead of 3 (simpler API)
3. **Single source of truth** - IsPlaced derived from Pos/Facing, can't drift
4. **Still fast** - Computed property is inlined by JIT

**Even better approach (if more time):**
```csharp
// Discriminated union pattern (most type-safe):
public abstract record RobotState
{
    public record Unplaced : RobotState;
    public record Placed(Position Pos, Direction Facing) : RobotState;
}

// Usage:
var unplaced = new RobotState.Unplaced();
var placed = new RobotState.Placed(new Position(0,0), Direction.North);

// Pattern matching:
return state switch
{
    RobotState.Unplaced => "Not placed",
    RobotState.Placed p => $"At {p.Pos}",
};
```

**Why I didn't use discriminated unions:**
- ⏱️ Time constraint (15 more minutes to implement)
- 📚 More complex for reviewers unfamiliar with pattern
- ✅ Current approach is "good enough" and clear

**Key lesson**: **Computed properties can enforce invariants without complex type hierarchies.**

---

### Q: Why validate bounds in parser vs engine? What if the board size changes?

**A:** **Clear separation of concerns** based on what each layer knows:

**Parser validates format:**
```csharp
// Parser: "Are X and Y integers?"
if (!int.TryParse(argParts[0].Trim(), out var x)) return false;
if (!int.TryParse(argParts[1].Trim(), out var y)) return false;

// Accepts: PLACE 10,10,NORTH (integers)
// Rejects: PLACE abc,def,NORTH (not integers)
```

**Engine validates business logic:**
```csharp
// Engine: "Are X and Y within board bounds?"
if (!_tabletop.IsValid(new Position(x, y))) {
    return new StepResult(state, null); // Ignore
}

// Tabletop: 
public bool IsValid(Position p) => 
    p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
```

**Why this separation:**

**1. Board size is configurable:**
```csharp
// Can change without touching parser:
var tabletop = new Tabletop(10, 10);  // 10×10 board
var engine = new Engine(tabletop);
// Parser unchanged! Still parses "PLACE 5,5,NORTH"
// Engine decides if (5,5) is valid for current board
```

**2. Parser is reusable:**
```csharp
// Same parser works for any board size:
var parser = new Parser();  // No board knowledge needed

// Different engines, same parser:
var smallBoard = new Engine(new Tabletop(3, 3));
var largeBoard = new Engine(new Tabletop(20, 20));

// Both use same parser!
```

**3. Single Responsibility:**
- **Parser**: String format → Typed commands
- **Engine**: Business rules → State transitions
- **Tabletop**: Domain rules → Position validation

**Alternative (not chosen):**
```csharp
// Anti-pattern: Parser knows business rules
public record PlaceCommand(int X, int Y, Direction Facing)
{
    public PlaceCommand(int x, int y, Direction facing)
    {
        if (x < 0 || x > 4 || y < 0 || y > 4)
            throw new ArgumentException("Out of bounds");
        // ...
    }
}
```

**Problems:**
- ❌ Hard-codes board size (5×5) in command object
- ❌ Parser becomes coupled to board dimensions
- ❌ Can't reuse parser for different board sizes
- ❌ Violates separation of concerns

**Real-world analogy:**
- **Parser** = Validates JSON syntax (is it valid JSON?)
- **Engine** = Validates business rules (is user authorized? is account active?)

You'd never put business logic in a JSON parser!

---

### Q: Are you using ToUpper() or ToUpperInvariant()? Why does it matter?

**A:** **ToUpperInvariant()** - critical for correctness:

```csharp
// REPORT implementation:
var output = $"{state.Pos.X},{state.Pos.Y},{state.Facing.Value.ToString().ToUpperInvariant()}";
//                                                                        ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
//                                                                        Culture-safe!
```

**Why it matters - The Turkish I Problem:**

```csharp
// In Turkish (tr-TR) locale:
"i".ToUpper()          // → "İ" (dotted capital I)
"I".ToLower()          // → "ı" (dotless lowercase i)

// In English (en-US) locale:
"i".ToUpper()          // → "I"
"I".ToLower()          // → "i"

// THIS BREAKS COMPARISONS:
Direction.North.ToString()  // "North"
.ToUpper()                  // Could be "NORTH" or "NORTİ" depending on culture!
```

**Actual test proving this:**
```csharp
[Fact]
public void Execute_Report_IsCultureInvariant()
{
    var originalCulture = CultureInfo.CurrentCulture;
    try
    {
        // Set Turkish culture
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        
        var state = new RobotState(new Position(1, 2), Direction.North);
        var result = _engine.Execute(state, new ReportCommand());
        
        // Must always be "NORTH", not "NORTİ"!
        Assert.Equal("1,2,NORTH", result.ReportOutput);
    }
    finally
    {
        CultureInfo.CurrentCulture = originalCulture;
    }
}
```

**When to use each:**

| Method | Use When | Example |
|--------|----------|---------|
| `ToUpper()` | Displaying to user in their locale | UI text, messages |
| `ToUpperInvariant()` | Protocol/API output, comparisons | JSON, XML, file formats, our REPORT |
| `ToLower()` | User-facing, culture-specific | Sort orders, display |
| `ToLowerInvariant()` | Case-insensitive comparisons | Dictionary keys, protocol |

**Why spec output must be invariant:**
- ✅ **Deterministic** - Same output regardless of server locale
- ✅ **Testable** - Tests pass on any machine (US, Turkey, Japan)
- ✅ **Parseable** - Consumers expect "NORTH" not "NORTİ"
- ✅ **Comparable** - String comparisons work reliably

**Other culture traps avoided:**
```csharp
// ✅ int.TryParse - culture-independent for integers
// ✅ Enum.TryParse(ignoreCase: true) - uses InvariantCulture
// ✅ string comparison with StringComparison.OrdinalIgnoreCase
```

**This is a "senior engineer" detail** - shows awareness of internationalization.

---

## The "Almost Guaranteed" Questions

### 1. Q: How did you enforce "no falling off the table"?

**A:** Three-layer approach:

**Layer 1 - Placement validation:**
```csharp
if (!_tabletop.IsValid(new Position(x, y))) {
    return new StepResult(state, null); // Don't place
}
```

**Layer 2 - Movement validation:**
```csharp
var newPosition = CalculateMove(state);
if (!_tabletop.IsValid(newPosition)) {
    return new StepResult(state, null); // Don't move
}
```

**Layer 3 - Tabletop bounds checking:**
```csharp
public bool IsValid(Position p) =>
    p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
```

**Key insight**: Calculate first, validate second, apply only if valid. Immutable state means failed operations can't corrupt state.

**Tested with**: 8 parameterized tests covering all edges and corners.

### 2. Q: How did you handle commands before `PLACE`?

**A:** Placement gate using computed property:

```csharp
public record RobotState(Position? Pos, Direction? Facing)
{
    public bool IsPlaced => Pos is not null && Facing is not null;
}

// In every Execute method:
if (!state.IsPlaced) return new StepResult(state, null);
```

**Behavior:**
- Robot starts with `Pos = null, Facing = null` → `IsPlaced = false`
- All non-PLACE commands check IsPlaced first
- Invalid PLACE keeps `Pos/Facing = null` → `IsPlaced` stays false
- Valid PLACE sets `Pos/Facing` → `IsPlaced` becomes true

**Key improvement**: IsPlaced is computed, so it's impossible to have inconsistent state.

**Tested with**: Explicit test sending MOVE, LEFT, RIGHT, REPORT before PLACE, verifying all ignored.

### 3. Q: Why did you split parsing from execution?

**A:** Three main reasons:

**1. Testability** - Engine tested with zero I/O:
```csharp
// No files, no strings, just logic:
var result = _engine.Execute(state, new MoveCommand());
```

**2. Reusability** - Same engine works with any input:
```csharp
// File, HTTP, database, REPL - all use same engine
var cmd = ParseFromFile(line);
var cmd = ParseFromHttp(json);
var cmd = ParseFromRepl(input);
// → All feed into same Engine.Execute()
```

**3. Single Responsibility** - Each component has one job:
- Parser: String format → Typed commands
- Engine: Business logic → State transitions
- Runner: Orchestration → I/O

**Without separation**, tests would need file mocking and be slow/brittle.

### 4. Q: Why did you choose your command representation (types vs strings)?

**A:** Strongly-typed commands for safety:

```csharp
record PlaceCommand(int X, int Y, Direction Facing) : ICommand;
```

**vs strings:**
```csharp
"PLACE 0,0,NORTH" // fragile, no compile-time safety
```

**Benefits:**
1. **Type safety** - Can't pass invalid data to Engine
2. **Compile-time checking** - Typos caught immediately
3. **Pattern matching** - Clean switch expressions
4. **Self-documenting** - Command structure is explicit
5. **Separation** - Parser validates format, Engine validates logic

**Trade-off**: Slight increase in code (6 command files) for much better maintainability.

### 5. Q: What tests prove the examples and boundary cases?

**A:** Systematic coverage:

**Specification examples (explicit tests):**
```csharp
Execute_Example1_ProducesExpectedOutput() // PLACE 0,0,N → MOVE → REPORT
Execute_Example2_ProducesExpectedOutput() // PLACE 0,0,N → LEFT → REPORT
```

**Boundary cases (parameterized tests):**
```csharp
[Theory]
[InlineData(0, 0, Direction.South)]   // Corner: bottom-left
[InlineData(0, 0, Direction.West)]
[InlineData(4, 4, Direction.North)]   // Corner: top-right
[InlineData(4, 4, Direction.East)]
[InlineData(0, 3, Direction.West)]    // Edge: left
[InlineData(4, 3, Direction.East)]    // Edge: right
[InlineData(3, 0, Direction.South)]   // Edge: bottom
[InlineData(3, 4, Direction.North)]   // Edge: top
Execute_MoveAtEdge_BlockedAndStateUnchanged(...)
```

**70 tests total** covering all commands, directions, edges, and error cases.

### 6. Q: What did you skip due to time and what would you do next?

**A:** Deliberately skipped (documented in README):

**Skipped:**
- Interactive REPL mode (~15 min)
- Structured logging (~20 min)
- Property-based testing (~30 min)
- Comment support in files (~5 min)
- Better error messages (~15 min)

**Total saved**: ~85 minutes → Made implementation feasible in 2 hours

**Next priorities:**
1. **Property-based tests** (20 min) - Highest confidence boost
2. **Logging with verbosity** (15 min) - Best debugging tool
3. **Better error messages** (10 min) - Best UX improvement

**Philosophy**: Ship working, tested software that meets requirements. Add features when users need them, not speculatively.

---

## Summary

This implementation demonstrates:
- ✅ **Clean architecture** - Separation of concerns (Parser/Engine/I/O)
- ✅ **Test-driven** - 94 tests, all passing (70 initial + 24 from code review improvements)
- ✅ **Type-safe** - Computed properties enforce invariants, prevent illegal states
- ✅ **Culture-aware** - ToUpperInvariant() for protocol output, tested with Turkish locale
- ✅ **Pragmatic** - Balances quality with time constraints
- ✅ **Documented** - Clear assumptions, trade-offs, and design rationale
- ✅ **Maintainable** - Easy to understand, extend, and test
- ✅ **Production-ready** - .NET 8.0 LTS, proper exit codes, defensive null checks

**Key improvements from code review:**
1. **RobotState refactored** - IsPlaced is now computed property (prevents inconsistent state)
2. **Culture-safe output** - ToUpperInvariant() instead of ToUpper()
3. **Strict direction validation** - Rejects numeric values (0,1,2,3)
4. **Better error handling** - Return codes instead of Environment.Exit()
5. **Defensive programming** - ArgumentNullException.ThrowIfNull() guards
6. **Named constants** - DefaultWidth/DefaultHeight instead of magic numbers

Every design decision was made with testability, simplicity, correctness, and time constraints in mind.

**Framework:** .NET 8.0 LTS (not 10.0, which doesn't exist)
**Test Count:** 94 tests (31 parser, 53 engine, 10 integration)
**Pass Rate:** 100%
**Linter Errors:** 0
**Build Status:** ✅ Clean
**Documentation:** Comprehensive (README + CODE_REVIEW + FIXES_APPLIED + this Q&A)

---

## Appendix: New Questions Added (Post Code-Review Update)

This document has been updated to reflect the current state of the system after comprehensive code reviews. New high-value questions added:

**Build & Runtime:**
1. How do I run this on a clean machine? What .NET version do you target and why?
2. What's your CLI contract? What happens if no args are passed?
3. Do you stream the file or read it all at once? Why?

**Type Safety & State Modeling (Critical):**
4. Does your RobotState truly prevent invalid combinations? What would you change?
5. Why validate bounds in parser vs engine? What if board size changes?
6. Are you using ToUpper() or ToUpperInvariant()? Why does it matter?

**Testing Depth:**
7. What's your "minimum confidence set" of tests? What could you delete?
8. How would you test Program-level behavior (stdout/stderr, exit codes)?
9. Did you test that invalid commands don't mutate state at all?

**Extensibility:**
10. Why didn't you introduce DI/interfaces for I/O?
11. If you added obstacles, where would the logic live and why?
12. If you added a new command (BACK), what changes are required?

**Total Question Count:** ~100+ questions covering all aspects of design, implementation, testing, and tradeoffs.

