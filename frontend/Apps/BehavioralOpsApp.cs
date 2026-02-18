namespace Frontend.Apps;

[App(icon: Icons.LayoutDashboard, title: "Behavioral Ops")]
public class BehavioralOpsApp : ViewBase
{
  public override object? Build()
  {
    var activePage = UseState("builder");
    var cohorts = UseState(new Cohort[]
    {
        new("Active US Users",      3, 8919, "2025-08-20", "High-Value", Array.Empty<string>()),
        new("Recent Signups (EU)",  1, 8380, "2025-08-21", "New", Array.Empty<string>()),
        new("New Prospect Cohort",  1, 6700, "2025-08-22", "New", Array.Empty<string>()),
    });

    object GetPageContent() => activePage.Value switch
    {
      "builder" => new BuilderView(cohorts, page => activePage.Value = page),
      "dashboard" => new DashboardView(),
      "library" => new LibraryView(cohorts, navigateTo: page => activePage.Value = page),
      "testing" => new TestingView(),
      _ => new BuilderView(cohorts, page => activePage.Value = page)
    };

    var header = new Card(
        Layout.Horizontal().Gap(3).Align(Align.Center).Width(Size.Full())
            | Text.H4("Behavioral Ops")
            | new Spacer()
            | new Button("Builder")
                .Variant(activePage.Value == "builder" ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => activePage.Value = "builder")
            | new Button("Dashboard")
                .Variant(activePage.Value == "dashboard" ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => activePage.Value = "dashboard")
            | new Button("Library")
                .Variant(activePage.Value == "library" ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => activePage.Value = "library")
            | new Button("Testing")
                .Variant(activePage.Value == "testing" ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => activePage.Value = "testing")
    );

    return new HeaderLayout(header, GetPageContent());
  }
}
