namespace Frontend.Apps;

using System.Net.Http.Json;
using System.Net.Http;

public class DashboardView : ViewBase
{
  public override object? Build()
  {
    var selectedTab = UseState(0);

    // --- Stat data ---
    var totalCohorts = UseState(0);
    var totalUsers = UseState(0);
    var avgSize = UseState(0.0);
    var sizeDistData = UseState(Array.Empty<DistributionItem>());
    var trendData = UseState(Array.Empty<TrendItem>());
    var activityData = UseState(Array.Empty<ActivityItem>());

    UseEffect(async () =>
    {
      try
      {
        using var http = new HttpClient();
        var stats = await http.GetFromJsonAsync<StatsResponse>("http://localhost:5152/api/analytics/stats");
        if (stats != null)
        {
          totalCohorts.Set(stats.totalCohorts);
          totalUsers.Set(stats.totalUsers);
          avgSize.Set(stats.avgSize);
        }

        var dist = await http.GetFromJsonAsync<DistributionItem[]>("http://localhost:5152/api/analytics/distribution");
        if (dist != null) sizeDistData.Set(dist);

        var trend = await http.GetFromJsonAsync<TrendItem[]>("http://localhost:5152/api/analytics/trend");
        if (trend != null) trendData.Set(trend);

        var activity = await http.GetFromJsonAsync<ActivityItem[]>("http://localhost:5152/api/analytics/activity");
        if (activity != null) activityData.Set(activity);
      }
      catch { }
    }, []);

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
        | StatCard(Icons.LayoutDashboard, "Total Cohorts", totalCohorts.Value.ToString())
        | StatCard(Icons.Users, "Total Users", ((int)totalUsers.Value).ToString("N0"))
        | StatCard(Icons.ChartBar, "Avg. Cohort Size", ((double)avgSize.Value).ToString("N2"));

    // --- Charts row ---
    var barChart = new Card(
        Layout.Vertical().Gap(2)
            | (Layout.Horizontal().Gap(2).Align(Align.Center)
                | Text.P("Cohort Size Distribution")
                | new Spacer()
                | Text.P("Hover over a bar to see details").Muted().Small())
            | sizeDistData.Value.ToBarChart()
                .Dimension("Range", e => e.range)
                .Measure("Count", e => e.Sum(f => f.count))
    );

    var lineChart = new Card(
        Layout.Vertical().Gap(2)
            | Text.P("Cohort Creation Trend")
            | trendData.Value.ToLineChart()
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
            | activityData.Value.ToTable()
                .Width(Size.Full())
                .Header(p => p.Cohort, "Cohort Name")
                .Header(p => p.Action, "Action")
                .Header(p => p.Timestamp, "Time")
                .Header(p => p.User, "User")
    );

    var content = selectedTab.Value == 0
        ? (object)(Layout.Vertical().Gap(6)
            | statsRow
            | chartsRow
            | activityFeed)
        : Layout.Vertical().Align(Align.Center).Padding(20)
            | Text.H3($"Content for {selectedTab.Value} tab").Muted();

    return Layout.Vertical().Gap(0)
        | Layout.Vertical().Gap(6).Padding(6)
            | content;
  }
}
