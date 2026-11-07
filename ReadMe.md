A C# console application that retrieves employee time-entry data from a REST API and generates reports showing total working hours.

## Technologies Used
- C#
- .NET 10
- REST API
- JSON Deserialization
- ScottPlot
- HTML and CSS

## Tasks Implemented

### Task A – Employee Working Hours Report
- Retrieves employee time-entry data.
- Calculates total working hours for each employee.
- Sorts employees in descending order of working hours.
- Highlights employees with fewer than 100 hours in red.
- Generates an HTML report.

### Task B – Employee Working Hours Pie Chart
- Calculates each employee's percentage of total working hours.
- Generates a multicolored pie chart.
- Displays employee names and percentages in the legend.
- Saves the chart as a PNG image.

## Output Files
- Outputs/EmployeeReport.html
- Outputs/EmployeePieChart.png

## How to Run

Configure the API URL using the RAPIDD_API_URL environment variable.

PowerShell:

```powershell
$env:RAPIDD_API_URL = "YOUR_API_URL"
```

Run the executable:

```powershell
.\RAPIDD-CSharp-Assignment.exe
```

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Publish
```
