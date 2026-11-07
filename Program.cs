
using System.Text;
using System.Text.Json;
using System.Net;
using ScottPlot;

class Program
{
    static async Task Main()
    {
        try
        {
            
            string apiUrl = Environment.GetEnvironmentVariable(
                "RAPIDD_API_URL")
                ?? throw new Exception("API URL not configured.");

            using HttpClient client = new HttpClient();

            Console.WriteLine("Connecting to RAPIDD API...");

            
            string json = await client.GetStringAsync(apiUrl);

            Console.WriteLine("Data received successfully!");

            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<TimeEntry> entries =
                JsonSerializer.Deserialize<List<TimeEntry>>(
                    json, options) ?? new List<TimeEntry>();

            
            var employees = entries
                .Where(e =>
                    !string.IsNullOrWhiteSpace(e.EmployeeName) &&
                    e.DeletedOn == null &&
                    e.EndTimeUtc > e.StarTimeUtc)
                .GroupBy(e => e.EmployeeName!.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(g => new EmployeeSummary
                {
                    Name = g.Key,
                    TotalHours = g.Sum(e =>
                        (e.EndTimeUtc!.Value -
                         e.StarTimeUtc!.Value).TotalHours)
                })
                .OrderByDescending(e => e.TotalHours)
                .ThenBy(e => e.Name)
                .ToList();

            if (employees.Count == 0)
            {
                Console.WriteLine("No valid employee records found.");
                return;
            }

            
            string outputPath = Path.Combine(
                Directory.GetCurrentDirectory(), "Outputs");

            Directory.CreateDirectory(outputPath);

            
            GenerateHtml(employees,Path.Combine(outputPath, "EmployeeReport.html"));

            
            GeneratePieChart(employees,Path.Combine(outputPath, "EmployeePieChart.png"));

            
            Console.WriteLine("\nEmployee Working Hours");

            foreach (var employee in employees)
            {
                Console.WriteLine(
                    $"{employee.Name}: {employee.TotalHours:F2} hours");
            }

            Console.WriteLine("\nBoth output files generated!");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        Console.WriteLine("\nPress Enter to exit.");
        Console.ReadLine();
    }

    
    static void GenerateHtml(
        List<EmployeeSummary> employees, string filePath)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <title>Employee Working Hours</title>

            <style>
                body {
                    font-family: Arial;
                    margin: 40px;
                    background-color: #f4f6f9;
                }

                h1 {
                    text-align: center;
                    color: #26364a;
                }

                table {
                    width: 80%;
                    margin: auto;
                    border-collapse: collapse;
                    background: white;
                }

                th, td {
                    padding: 14px;
                    border: 1px solid #ddd;
                    text-align: center;
                }

                th {
                    background-color: #26364a;
                    color: white;
                }

                tr.low-hours {
                    background-color: #f70808;
                    color: white;
                }

                tr:nth-child(even):not(.low-hours) {
                    background-color: #edf1f5;
                }
            </style>
        </head>

        <body>
            <h1>Employee Working Hours Report</h1>

            <table>
                <tr>
                    <th>Employee Name</th>
                    <th>Total Hours Worked</th>
                </tr>
        """);

        foreach (var employee in employees)
        {
            string cssClass =
                employee.TotalHours < 100
                ? "class=\"low-hours\""
                : "";

            html.AppendLine($"""
                <tr {cssClass}>
                    <td>{WebUtility.HtmlEncode(employee.Name)}</td>
                    <td>{employee.TotalHours:F2}</td>
                </tr>
            """);
        }

        html.AppendLine("""
            </table>
        </body>
        </html>
        """);

        File.WriteAllText(filePath, html.ToString(), Encoding.UTF8);

        Console.WriteLine("HTML report created successfully!");
    }

    
    static void GeneratePieChart(
        List<EmployeeSummary> employees, string filePath)
    {
        double totalHours = employees.Sum(e => e.TotalHours);

        if (totalHours <= 0)
            throw new Exception("No positive working hours available.");

        List<PieSlice> slices = new List<PieSlice>();

        ScottPlot.Color[] colors =
        {
            ScottPlot.Color.FromHex("#4472C4"),
            ScottPlot.Color.FromHex("#ED7D31"),
            ScottPlot.Color.FromHex("#70AD47"),
            ScottPlot.Color.FromHex("#C00000"),
            ScottPlot.Color.FromHex("#7030A0"),
            ScottPlot.Color.FromHex("#00B0F0"),
            ScottPlot.Color.FromHex("#FFC000"),
            ScottPlot.Color.FromHex("#A5A5A5"),
            ScottPlot.Color.FromHex("#FF6699"),
            ScottPlot.Color.FromHex("#548235")
        };

        for (int i = 0; i < employees.Count; i++)
        {
            var employee = employees[i];

            double percentage =
                employee.TotalHours / totalHours * 100;

            slices.Add(new PieSlice
            {
                Value = employee.TotalHours,
                Label = $"{percentage:F1}%",
                LegendText = $"{employee.Name} ({percentage:F1}%)",
                FillColor = colors[i % colors.Length]
            });
        }

        ScottPlot.Plot plot = new ScottPlot.Plot();



        var pie = plot.Add.Pie(slices);

        pie.SliceLabelDistance = 0.5;

        foreach (var slice in pie.Slices)
        {
            slice.LabelFontSize = 14;
            slice.LabelBold = true;
        }
        LegendItem legendTitle = new LegendItem()
        {
            LabelText = "Employee Name & Percentage",
            LabelFontSize = 13,
            LabelBold = true,
            LabelOffsetX = -20
        };

        plot.Legend.ManualItems.Add(legendTitle);

        plot.ShowLegend();
        plot.Axes.Frameless();
        plot.Title("Employee Working Hours Distribution");

        plot.Axes.Title.Label.ForeColor =
            ScottPlot.Color.FromHex("#1F4E79");

        plot.Axes.Title.Label.FontSize = 26;

        plot.Axes.Title.Label.Bold = true;
        plot.HideGrid();

        plot.SavePng(filePath, 1000, 700);

        Console.WriteLine("Pie chart created successfully!");
    }
}


public class TimeEntry
{
    public string? EmployeeName { get; set; }

    public DateTime? StarTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public DateTime? DeletedOn { get; set; }
}


public class EmployeeSummary
{
    public string Name { get; set; } = "";

    public double TotalHours { get; set; }
}
