namespace RobotSim;

/// <summary>
/// Result of executing a command, containing the new state and optional report output.
/// </summary>
public record StepResult(RobotState NewState, string? ReportOutput);

