namespace Frontend.Apps;

using System.Net.Http.Json;
using System.Text.Json;
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

    Console.WriteLine($"[Frontend] DashboardView Build called. Tab: {selectedTab.Value}, Dist: {sizeDistData.Value.Length}, Trend: {trendData.Value.Length}");

    UseEffect(async () =>
    {
      try
      {
        using var http = new HttpClient();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var statsJson = await http.GetStringAsync("http://localhost:5152/api/analytics/stats");
        Console.WriteLine($"[Frontend] Stats JSON: {statsJson}");
        var stats = JsonSerializer.Deserialize<StatsResponse>(statsJson, options);
        if (stats != null)
        {
          totalCohorts.Set(stats.totalCohorts);
          totalUsers.Set(stats.totalUsers);
          avgSize.Set(stats.avgSize);
        }

        var distJson = await http.GetStringAsync("http://localhost:5152/api/analytics/distribution");
        Console.WriteLine($"[Frontend] Dist JSON: {distJson}");
        var dist = JsonSerializer.Deserialize<DistributionItem[]>(distJson, options);
        if (dist != null) sizeDistData.Set(dist);

        var trendJson = await http.GetStringAsync("http://localhost:5152/api/analytics/trend");
        Console.WriteLine($"[Frontend] Trend JSON: {trendJson}");
        var trend = JsonSerializer.Deserialize<TrendItem[]>(trendJson, options);
        if (trend != null) trendData.Set(trend);

        var activityJson = await http.GetStringAsync("http://localhost:5152/api/analytics/activity");
        Console.WriteLine($"[Frontend] Activity JSON: {activityJson}");
        var activity = JsonSerializer.Deserialize<ActivityItem[]>(activityJson, options);
        if (activity != null) activityData.Set(activity);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Frontend] ERROR fetching dashboard data: {ex.GetType().Name} - {ex.Message}");
      }
    }, []);

    // --- Stat cards ---
    object StatCard(Icons icon, string label, string value) =>
        new Card(
            Layout.Horizontal().Gap(4).Align(Align.Center)
                | new Icon(icon)
                | (Layout.Vertical().Gap(1)
                    | Text.P(label).Muted().Small()
                    | Text.H2(value))
        );

    var statsRow = Layout.Grid().Columns(3).Gap(4)
        | StatCard(Icons.LayoutDashboard, "Total Cohorts", totalCohorts.Value.ToString())
        | StatCard(Icons.Users, "Total Users", ((int)totalUsers.Value).ToString("N0"))
        | StatCard(Icons.ChartBar, "Avg. Cohort Size", ((double)avgSize.Value).ToString("N2"));

    // --- Charts row ---
    var pieChart = new Card(
        Layout.Vertical().Gap(2)
            | (Layout.Horizontal().Gap(2).Align(Align.Center)
                | Text.P("Cohort Size Distribution")
                | new Spacer()
                | Text.P("Users by continent").Muted().Small())
            | sizeDistData.Value.ToPieChart(e => e.continent, e => (double)e.Sum(f => f.count))
    );

    var lineChart = new Card(
        Layout.Vertical().Gap(2)
            | Text.P("Cohort Creation Trend")
            | trendData.Value.ToLineChart()
                .Dimension("Month", e => e.month)
                .Measure("Logins", e => (double)e.Sum(f => f.logins))
                .Measure("Purchases", e => (double)e.Sum(f => f.purchases))
                .Measure("Signups", e => (double)e.Sum(f => f.signups))
                .Measure("Views", e => (double)e.Sum(f => f.views))
    );

    var chartsRow = Layout.Grid().Columns(2).Gap(4)
        | pieChart
        | lineChart;

    // --- Activity feed table ---
    var activityFeed = new Card(
        Layout.Vertical().Gap(3)
            | Text.P("Recent Activity")
            | activityData.Value.ToTable()
                .Width(Size.Full())
                .Header(p => p.cohort, "Cohort Name")
                .Header(p => p.action, "Action")
                .Header(p => p.timestamp, "Time")
                .Header(p => p.user, "User")
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
