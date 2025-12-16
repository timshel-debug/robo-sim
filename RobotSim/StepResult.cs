namespace RobotSim;

/// <summary>
/// Result of executing a command, containing the new state and optional report output.
/// </summary>
/// <param name="NewState">The new robot state after command execution.</param>
/// <param name="ReportOutput">The output string if the command was REPORT; otherwise null.</param>
public record StepResult(RobotState NewState, string? ReportOutput);

