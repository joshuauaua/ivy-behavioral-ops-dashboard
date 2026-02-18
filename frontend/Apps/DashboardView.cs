namespace Frontend.Apps;

public class DashboardView : ViewBase
{
  public override object? Build()
  {
    var selectedTab = UseState(0);

    // --- Stat data ---
    int totalCohorts = 12;
    int totalUsers = 45820;
    double avgSize = 3818.33;

    // --- Bar chart data: Cohort Size Distribution ---
    var sizeDistData = new[]
    {
            new { Range = "0-1k",   Count = 3 },
            new { Range = "1k-5k",  Count = 5 },
            new { Range = "5k-10k", Count = 2 },
            new { Range = "10k+",   Count = 2 },
        };

    // --- Line chart data: Cohort Creation Trend ---
    var trendData = new[]
    {
            new { Month = "Jan", Cohorts = 2 },
            new { Month = "Feb", Cohorts = 1 },
            new { Month = "Mar", Cohorts = 3 },
            new { Month = "Apr", Cohorts = 4 },
            new { Month = "May", Cohorts = 2 },
        };

    // --- Activity feed data ---
    var activitiesData = new[]
    {
            new { Cohort = "New Prospect Cohort", Action = "Updated",   Timestamp = "2 mins ago",  User = "joshuang" },
            new { Cohort = "Active US Users",     Action = "Exported",  Timestamp = "1 hour ago",  User = "joshuang" },
            new { Cohort = "Recent Signups (EU)", Action = "Created",   Timestamp = "3 hours ago", User = "admin" },
            new { Cohort = "New Prospect Cohort", Action = "Scheduled", Timestamp = "5 hours ago", User = "joshuang" },
            new { Cohort = "All Customers",       Action = "Created",   Timestamp = "Yesterday",   User = "system" },
        };

    // --- Stat cards ---
    object StatCard(Icons icon, string label, string value) =>
        new Card(
            Layout.Horizontal().Gap(4).Align(Align.Center)
                | new Card(new Icon(icon))
                | (Layout.Vertical().Gap(1)
                    | Text.P(label).Muted().Small()
                    | Text.H2(value))
        );

    var statsRow = Layout.Grid().Columns(3).Gap(4)
        | StatCard(Icons.LayoutDashboard, "Total Cohorts", totalCohorts.ToString())
        | StatCard(Icons.Users, "Total Users", totalUsers.ToString("N0"))
        | StatCard(Icons.ChartBar, "Avg. Cohort Size", avgSize.ToString("N2"));

    // --- Charts row ---
    var barChart = new Card(
        Layout.Vertical().Gap(2)
            | (Layout.Horizontal().Gap(2).Align(Align.Center)
                | Text.P("Cohort Size Distribution")
                | new Spacer()
                | Text.P("Hover over a bar to see details").Muted().Small())
            | sizeDistData.ToBarChart()
                .Dimension("Range", e => e.Range)
                .Measure("Count", e => e.Sum(f => f.Count))
    );

    var lineChart = new Card(
        Layout.Vertical().Gap(2)
            | Text.P("Cohort Creation Trend")
            | trendData.ToLineChart()
                .Dimension("Month", e => e.Month)
                .Measure("Cohorts", e => e.Sum(f => f.Cohorts))
    );

    var chartsRow = Layout.Grid().Columns(2).Gap(4)
        | barChart
        | lineChart;

    // --- Activity feed table ---
    var activityFeed = new Card(
        Layout.Vertical().Gap(3)
            | Text.P("Recent Activity")
            | activitiesData.ToTable()
                .Width(Size.Full())
                .Header(p => p.Cohort, "Cohort Name")
    );

    var content = selectedTab.Value == 0
        ? (object)(Layout.Vertical().Gap(6)
            | statsRow
            | chartsRow
            | activityFeed)
        : Layout.Vertical().Align(Align.Center).Padding(20)
            | Text.H3($"Content for {selectedTab.Value} tab").Muted();

    return Layout.Vertical().Gap(0)
        | new DashboardHeader(selectedTab, () => { /* Handle click */ })
        | Layout.Vertical().Gap(6).Padding(6)
            | content;
  }
}
