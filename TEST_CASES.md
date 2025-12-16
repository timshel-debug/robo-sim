# Test Case Matrix

This document provides a comprehensive list of all test cases implemented in the RobotSim.Tests project, organized by category as specified in the requirements.

## Test Category A: Parser Tests (Unit)

Tests the parsing of command strings into ICommand objects.

| ID | Test Name | Input | Expected Result | Status |
|----|-----------|-------|-----------------|--------|
| A1 | Parse PLACE command | `PLACE 0,0,NORTH` | `PlaceCommand(0,0,North)` | ✅ Pass |
| A2 | Parse PLACE with whitespace | `PLACE 1, 2, WEST` | `PlaceCommand(1,2,West)` | ✅ Pass |
| A3a | Parse MOVE | `MOVE` | `MoveCommand` | ✅ Pass |
| A3b | Parse LEFT | `LEFT` | `LeftCommand` | ✅ Pass |
| A3c | Parse RIGHT | `RIGHT` | `RightCommand` | ✅ Pass |
| A3d | Parse REPORT | `REPORT` | `ReportCommand` | ✅ Pass |
| A4a | Case-insensitive lowercase | `place 0,0,north` | `PlaceCommand(0,0,North)` | ✅ Pass |
| A4b | Case-insensitive mixed case | `Place 0,0,North` | `PlaceCommand(0,0,North)` | ✅ Pass |
| A4c | Case-insensitive uppercase | `PLACE 0,0,NORTH` | `PlaceCommand(0,0,North)` | ✅ Pass |
| A4d | Case-insensitive commands | `move`, `MOVE`, `Move` | Respective commands | ✅ Pass |
| A5a | Invalid PLACE - no args | `PLACE` | Parse fails | ✅ Pass |
| A5b | Invalid PLACE - missing direction | `PLACE 1,2` | Parse fails | ✅ Pass |
| A5c | Invalid PLACE - non-numeric coords | `PLACE a,b,NORTH` | Parse fails | ✅ Pass |
| A5d | Invalid PLACE - invalid direction | `PLACE 1,2,NOPE` | Parse fails | ✅ Pass |
| A5e | Invalid PLACE - too many args | `PLACE 1,2,3,NORTH` | Parse fails | ✅ Pass |
| A5f | Invalid PLACE - missing X | `PLACE ,1,NORTH` | Parse fails | ✅ Pass |
| A6a | Unknown command - JUMP | `JUMP` | Parse fails | ✅ Pass |
| A6b | Unknown command - FLY | `FLY` | Parse fails | ✅ Pass |
| A6c | Unknown command - INVALID | `INVALID` | Parse fails | ✅ Pass |
| A6d | Unknown command - empty string | `""` | Parse fails | ✅ Pass |
| A6e | Unknown command - whitespace only | `"   "` | Parse fails | ✅ Pass |
| A7a | All directions - NORTH | `PLACE 0,0,NORTH` | Direction.North | ✅ Pass |
| A7b | All directions - EAST | `PLACE 0,0,EAST` | Direction.East | ✅ Pass |
| A7c | All directions - SOUTH | `PLACE 0,0,SOUTH` | Direction.South | ✅ Pass |
| A7d | All directions - WEST | `PLACE 0,0,WEST` | Direction.West | ✅ Pass |

**Total Parser Tests: 24**

## Test Category B: Engine Tests (Unit) — Core Rules

Tests the execution logic and state transitions.

### B.1 Placement Gate / Ignore Behavior

| ID | Test Name | Commands | Expected Behavior | Status |
|----|-----------|----------|-------------------|--------|
| B1a | MOVE before placement | Unplaced → `MOVE` | State remains unplaced, no output | ✅ Pass |
| B1b | LEFT before placement | Unplaced → `LEFT` | State remains unplaced | ✅ Pass |
| B1c | REPORT before placement | Unplaced → `REPORT` | State remains unplaced, no output | ✅ Pass |
| B2a | Invalid PLACE (5,5) | `PLACE 5,5,NORTH` | Robot remains unplaced | ✅ Pass |
| B2b | Invalid PLACE (-1,0) | `PLACE -1,0,NORTH` | Robot remains unplaced | ✅ Pass |
| B2c | Invalid PLACE (0,-1) | `PLACE 0,-1,NORTH` | Robot remains unplaced | ✅ Pass |
| B2d | Invalid PLACE (5,0) | `PLACE 5,0,NORTH` | Robot remains unplaced | ✅ Pass |
| B2e | Invalid PLACE (0,5) | `PLACE 0,5,NORTH` | Robot remains unplaced | ✅ Pass |

### B.2 Placement and Bounds

| ID | Test Name | Commands | Expected State | Status |
|----|-----------|----------|----------------|--------|
| B3 | Valid PLACE sets state | `PLACE 0,0,NORTH` | IsPlaced=true, Pos=(0,0), Facing=North | ✅ Pass |
| B4 | Re-PLACE overwrites | `PLACE 0,0,NORTH` → `PLACE 4,4,SOUTH` | Pos=(4,4), Facing=South | ✅ Pass |

### B.3 Rotation

| ID | Test Name | Commands | Expected Direction | Status |
|----|-----------|----------|-------------------|--------|
| B5a | LEFT: N→W | `LEFT` from North | West | ✅ Pass |
| B5b | LEFT: W→S | `LEFT` from West | South | ✅ Pass |
| B5c | LEFT: S→E | `LEFT` from South | East | ✅ Pass |
| B5d | LEFT: E→N | `LEFT` from East | North (full cycle) | ✅ Pass |
| B6a | RIGHT: N→E | `RIGHT` from North | East | ✅ Pass |
| B6b | RIGHT: E→S | `RIGHT` from East | South | ✅ Pass |
| B6c | RIGHT: S→W | `RIGHT` from South | West | ✅ Pass |
| B6d | RIGHT: W→N | `RIGHT` from West | North (full cycle) | ✅ Pass |

### B.4 Movement

| ID | Test Name | Starting Pos/Dir | Expected Position | Status |
|----|-----------|------------------|-------------------|--------|
| B7a | MOVE North | (2,2,North) → `MOVE` | (2,3) - Y increments | ✅ Pass |
| B7b | MOVE South | (2,2,South) → `MOVE` | (2,1) - Y decrements | ✅ Pass |
| B7c | MOVE East | (2,2,East) → `MOVE` | (3,2) - X increments | ✅ Pass |
| B7d | MOVE West | (2,2,West) → `MOVE` | (1,2) - X decrements | ✅ Pass |

### B.5 Edge Blocking

| ID | Test Name | Position & Direction | Expected Behavior | Status |
|----|-----------|---------------------|-------------------|--------|
| B8a | Blocked at (0,0) South | (0,0,South) → `MOVE` | Stays at (0,0) | ✅ Pass |
| B8b | Blocked at (0,0) West | (0,0,West) → `MOVE` | Stays at (0,0) | ✅ Pass |
| B8c | Blocked at (4,4) North | (4,4,North) → `MOVE` | Stays at (4,4) | ✅ Pass |
| B8d | Blocked at (4,4) East | (4,4,East) → `MOVE` | Stays at (4,4) | ✅ Pass |
| B8e | Blocked at (0,y) West | (0,3,West) → `MOVE` | Stays at (0,3) | ✅ Pass |
| B8f | Blocked at (4,y) East | (4,3,East) → `MOVE` | Stays at (4,3) | ✅ Pass |
| B8g | Blocked at (x,0) South | (3,0,South) → `MOVE` | Stays at (3,0) | ✅ Pass |
| B8h | Blocked at (x,4) North | (3,4,North) → `MOVE` | Stays at (3,4) | ✅ Pass |

### B.6 Reporting

| ID | Test Name | State | Expected Output | Status |
|----|-----------|-------|-----------------|--------|
| B9 | REPORT output format | (0,1,North) → `REPORT` | `"0,1,NORTH"` | ✅ Pass |

### B.7 Specification Examples

| ID | Test Name | Command Sequence | Expected Output | Status |
|----|-----------|------------------|-----------------|--------|
| B10 | Example 1 | `PLACE 0,0,NORTH` → `MOVE` → `REPORT` | `"0,1,NORTH"` | ✅ Pass |
| B11 | Example 2 | `PLACE 0,0,NORTH` → `LEFT` → `REPORT` | `"0,0,WEST"` | ✅ Pass |

### B.8 Additional Tests

| ID | Test Name | Commands | Expected Behavior | Status |
|----|-----------|----------|-------------------|--------|
| B12 | Rotation doesn't change position | `LEFT` or `RIGHT` at (2,3) | Position remains (2,3) | ✅ Pass |
| B13a | Valid corner (0,0) | `PLACE 0,0,NORTH` | Placement succeeds | ✅ Pass |
| B13b | Valid corner (0,4) | `PLACE 0,4,NORTH` | Placement succeeds | ✅ Pass |
| B13c | Valid corner (4,0) | `PLACE 4,0,NORTH` | Placement succeeds | ✅ Pass |
| B13d | Valid corner (4,4) | `PLACE 4,4,NORTH` | Placement succeeds | ✅ Pass |
| B13e | Valid edge (2,0) | `PLACE 2,0,NORTH` | Placement succeeds | ✅ Pass |
| B13f | Valid edge (2,4) | `PLACE 2,4,NORTH` | Placement succeeds | ✅ Pass |
| B13g | Valid edge (0,2) | `PLACE 0,2,NORTH` | Placement succeeds | ✅ Pass |
| B13h | Valid edge (4,2) | `PLACE 4,2,NORTH` | Placement succeeds | ✅ Pass |

**Total Engine Tests: 45**

## Test Category C: Integration Tests (End-to-End)

Tests the complete flow from parsing to execution to output.

| ID | Test Name | Command Sequence | Expected Outputs | Status |
|----|-----------|------------------|------------------|--------|
| C1 | Example 1 integration | `PLACE 0,0,NORTH` → `MOVE` → `REPORT` | `["0,1,NORTH"]` | ✅ Pass |
| C2 | Example 2 integration | `PLACE 0,0,NORTH` → `LEFT` → `REPORT` | `["0,0,WEST"]` | ✅ Pass |
| C3 | Ignored commands before placement | `MOVE` → `LEFT` → `RIGHT` → `REPORT` → `PLACE 1,2,EAST` → `REPORT` | `["1,2,EAST"]` | ✅ Pass |
| C4 | Blocked moves at edges | `PLACE 0,0,SOUTH` → `MOVE` → `REPORT` → `LEFT` → `MOVE` → `REPORT` | `["0,0,SOUTH", "1,0,EAST"]` | ✅ Pass |
| C5 | Re-PLACE and continue | `PLACE 1,2,NORTH` → `MOVE` → `REPORT` → `PLACE 3,3,SOUTH` → `MOVE` → `REPORT` | `["1,3,NORTH", "3,2,SOUTH"]` | ✅ Pass |
| C6 | Invalid commands ignored | `INVALID` → `PLACE 0,0,NORTH` → `JUMP` → `MOVE` → `FLY` → `REPORT` | `["0,1,NORTH"]` | ✅ Pass |
| C7 | Complex scenario | `PLACE 1,1,NORTH` → 2×`MOVE` → `LEFT` → `MOVE` → `REPORT` → 2×`RIGHT` → `MOVE` → `REPORT` | `["0,3,WEST", "1,3,EAST"]` | ✅ Pass |
| C8 | Multiple reports | `PLACE 0,0,NORTH` → `REPORT` → `MOVE` → `REPORT` → `MOVE` → `REPORT` | `["0,0,NORTH", "0,1,NORTH", "0,2,NORTH"]` | ✅ Pass |
| C9 | Invalid PLACE ignored | `PLACE 5,5,NORTH` → `MOVE` → `PLACE 2,2,EAST` → `REPORT` | `["2,2,EAST"]` | ✅ Pass |

**Total Integration Tests: 9**

---

## Summary

| Category | Count | Status |
|----------|-------|--------|
| **Parser Tests (A)** | 24 | ✅ All Pass |
| **Engine Tests (B)** | 45 | ✅ All Pass |
| **Integration Tests (C)** | 9 | ✅ All Pass |
| **TOTAL** | **78** | **✅ All Pass** |

## Coverage Notes

### Functional Requirements Coverage

- ✅ **FR-01**: Tabletop validation (5×5 grid, origin at south-west)
- ✅ **FR-02**: All commands supported (PLACE, MOVE, LEFT, RIGHT, REPORT)
- ✅ **FR-03**: Initial placement gate enforced
- ✅ **FR-04**: Placement validation (bounds and direction checking)
- ✅ **FR-05**: Movement rules (forward movement with fall prevention)
- ✅ **FR-06**: Rotation rules (90-degree rotations without position change)
- ✅ **FR-07**: Reporting (correct format and output)
- ✅ **FR-08**: Ignore behavior when not placed

### Non-Functional Requirements Coverage

- ✅ **NFR-01**: Deterministic core (Engine is pure, no I/O)
- ✅ **NFR-02**: Separation of concerns (Parser, Engine, Runner)
- ✅ **NFR-03**: Robustness (invalid inputs handled gracefully)

### Edge Cases Tested

- ✅ All four corners of the tabletop
- ✅ All four edges of the tabletop
- ✅ All four directions for movement blocking
- ✅ Full rotation cycles (left and right)
- ✅ Commands before placement
- ✅ Invalid placement coordinates
- ✅ Invalid command strings
- ✅ Re-placement scenarios
- ✅ Case-insensitive input
- ✅ Whitespace variations

## Running Specific Test Categories

```bash
# Run all tests
dotnet test

# Run only Parser tests
dotnet test --filter "FullyQualifiedName~ParserTests"

# Run only Engine tests
dotnet test --filter "FullyQualifiedName~EngineTests"

# Run only Integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Run a specific test
dotnet test --filter "FullyQualifiedName~Execute_Example1_ProducesExpectedOutput"
```

## Test Implementation Details

All tests are implemented using:
- **xUnit** as the test framework
- **Standard Assert** methods for assertions
- **Theory/InlineData** for parameterized tests
- **Pure unit tests** for Parser and Engine (no I/O, no mocking needed)
- **Helper methods** in integration tests to simulate end-to-end flows

The tests are deterministic, fast, and can be run in any order or in parallel.

