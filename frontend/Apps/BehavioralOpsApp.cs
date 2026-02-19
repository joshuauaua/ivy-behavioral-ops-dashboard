namespace Frontend.Apps;

using System.Net.Http.Json;
using System.Net.Http;
using System.Text.Json;

[App(icon: Icons.LayoutDashboard, title: "Behavioral Ops")]
public class BehavioralOpsApp : ViewBase
{
  public override object? Build()
  {
    var selectedTabIndex = UseState(0);
    var cohorts = UseState(Array.Empty<Cohort>());
    var selectedCohort = UseState((Cohort?)null);

    UseEffect(async () =>
    {
      try
      {
        using var http = new HttpClient();
        var result = await http.GetFromJsonAsync<BackendCohort[]>("http://localhost:5152/api/cohorts");
        if (result != null)
        {
          var mapped = result.Select(c => new Cohort(
              c.name,
              1, // Version default
              new Random().Next(50, 200), // Simulated member count for now or fetch real
              c.createdAt.ToString("yyyy-MM-dd"),
              "New",
              JsonSerializer.Deserialize<string[]>(c.definition) ?? Array.Empty<string>(),
              Array.Empty<string>()
          )).ToArray();
          cohorts.Set(mapped);
        }
      }
      catch { }
    }, []);

    string GetActivePage() => selectedTabIndex.Value switch
    {
      0 => "dashboard",
      1 => "library",
      2 => "testing",
      4 => "builder",
      _ => "dashboard"
    };

    object GetPageContent() => GetActivePage() switch
    {
      "dashboard" => new DashboardView(),
      "library" => new LibraryView(cohorts, selectedCohort, _ => selectedTabIndex.Set(4)),
      "builder" => new BuilderView(cohorts, selectedCohort, page =>
      {
        if (page == "library") selectedTabIndex.Set(1);
      }),
      "testing" => new TestingView(),
      _ => new DashboardView()
    };

    var header = new DashboardHeader(selectedTabIndex, () =>
    {
      selectedCohort.Set((Cohort?)null);
      selectedTabIndex.Set(4);
    });

    return new HeaderLayout(header, GetPageContent());
  }
}
