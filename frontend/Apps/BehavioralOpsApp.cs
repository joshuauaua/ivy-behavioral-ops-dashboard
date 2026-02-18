namespace Frontend.Apps;

[App(icon: Icons.LayoutDashboard, title: "Behavioral Ops")]
public class BehavioralOpsApp : ViewBase
{
  public override object? Build()
  {
    var activePage = UseState("builder");

    object GetPageContent() => activePage.Value switch
    {
      "builder" => new BuilderView(),
      "dashboard" => new DashboardView(),
      "library" => new LibraryView(navigateTo: page => activePage.Value = page),
      "testing" => new TestingView(),
      _ => new BuilderView()
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
