namespace Frontend.Apps;

[App(icon: Icons.LayoutDashboard, title: "Behavioral Ops")]
public class BehavioralOpsApp : ViewBase
{
  public override object? Build()
  {
    var selectedTabIndex = UseState(0);
    var cohorts = UseState(new Cohort[]
    {
        new("Active US Users",      3, 8919, "2025-08-20", "High-Value", Array.Empty<string>(), Array.Empty<string>()),
        new("Recent Signups (EU)",  1, 8380, "2025-08-21", "New", Array.Empty<string>(), Array.Empty<string>()),
        new("New Prospect Cohort",  1, 6700, "2025-08-22", "New", Array.Empty<string>(), Array.Empty<string>()),
    });

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
      "library" => new LibraryView(cohorts, _ => selectedTabIndex.Set(4)), // Navigate to Templates (Builder)
      "builder" => new BuilderView(cohorts, page =>
      {
        if (page == "library") selectedTabIndex.Set(1);
      }),
      "testing" => new TestingView(),
      _ => new DashboardView()
    };

    var header = new DashboardHeader(selectedTabIndex, () => selectedTabIndex.Set(4));

    return new HeaderLayout(header, GetPageContent());
  }
}
