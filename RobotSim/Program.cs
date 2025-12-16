using RobotSim;

namespace RobotSim;

class Program
{
    static void Main(string[] args)
    {
        // Determine the file path
        string filePath;
        if (args.Length == 0)
        {
            filePath = "commands.txt";
        }
        else
        {
            filePath = args[0];
        }

        // Check if file exists
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File '{filePath}' not found.");
            Environment.Exit(1);
            return;
        }

        // Initialize components
        var tabletop = new Tabletop();
        var engine = new Engine(tabletop);
        var parser = new Parser();
        var state = RobotState.Initial();

        // Read and process commands
        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            // Try to parse the command
            if (!parser.TryParse(line, out var command) || command == null)
            {
                // Invalid or unknown command - ignore
                continue;
            }

            // Execute the command
            var result = engine.Execute(state, command);
            state = result.NewState;

            // If there's report output, print it
            if (result.ReportOutput != null)
            {
                Console.WriteLine(result.ReportOutput);
            }
        }
    }
}
