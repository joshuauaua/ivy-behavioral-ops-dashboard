namespace Frontend.Apps;

public class BuilderView : ViewBase
{
  readonly IState<Cohort[]> _cohorts;
  readonly Action<string> _navigateTo;

  public BuilderView(IState<Cohort[]> cohorts, Action<string> navigateTo)
  {
    _cohorts = cohorts;
    _navigateTo = navigateTo;
  }

  record BlockDef(string Id, string Label, string Description, Icons Icon, string Category);

  static readonly BlockDef[] AvailableBlocks =
  {
        new("user_login",      "User Login",      "Triggered when a user logs in.", Icons.LogIn, "EVENT"),
        new("purchase",        "Purchase",        "Occurs when a user completes a transaction.", Icons.ShoppingCart, "EVENT"),
        new("page_view",       "Page View",       "A user visits a specific page.", Icons.Eye, "EVENT"),
        new("user_signup",     "User Signup",     "A new user creates an account.", Icons.UserPlus, "EVENT"),
        new("region_asia",     "Region: Asia",    "Filter users located in Asia.", Icons.MapPin, "FILTER"),
        new("region_eu",       "Region: EU",      "Filter users located in Europe.", Icons.MapPin, "FILTER"),
        new("region_usa",      "Region: USA",     "Filter users located in the US.", Icons.MapPin, "FILTER"),
        new("churn_risk",      "Churn Risk",      "Users identified as likely to churn.", Icons.CircleAlert, "FILTER"),
        new("high_value",      "High Value",      "Users with high lifetime value.", Icons.CircleDollarSign, "FILTER"),
    };

  public override object? Build()
  {
    var canvasBlocks = UseState(Array.Empty<string>());
    var cohortName = UseState("New Prospect Cohort");
    var isEditingName = UseState(false);
    var client = UseService<IClientProvider>();

    // Realtime count (number of blocks added as a proxy)
    int count = canvasBlocks.Value.Length * 1247; // simulated count

    var titleContent = isEditingName.Value
        ? Layout.Horizontal().Gap(2).Align(Align.Center)
            | new TextInput(cohortName, placeholder: "Cohort name...")
            | new Button().Icon(Icons.Check).Variant(ButtonVariant.Ghost).HandleClick(_ => isEditingName.Set(false))
        : Layout.Horizontal().Gap(2).Align(Align.Center)
            | Text.H3(cohortName.Value)
            | new Button().Icon(Icons.Pencil).Variant(ButtonVariant.Ghost).HandleClick(_ => isEditingName.Set(true));

    var headerCard = new Card(
        Layout.Vertical().Gap(4).Width(Size.Full())
            | (Layout.Vertical().Gap(1)
                | titleContent
                | Text.P("Define your target audience by combining filters.").Muted())
            | (Layout.Horizontal().Gap(4).Align(Align.Center).Width(Size.Full())
                | (Layout.Vertical().Gap(2)
                    | (Layout.Horizontal().Gap(2).Width(Size.Full())
                        | new Button("Save Cohort").Variant(ButtonVariant.Primary).Icon(Icons.Save).Width(Size.Full())
                            .HandleClick(_ =>
                            {
                              var newCohort = new Cohort(
                                  cohortName.Value,
                                  1,
                                  count,
                                  DateTime.Now.ToString("yyyy-MM-dd"),
                                  "New",
                                  canvasBlocks.Value
                              );
                              _cohorts.Set([.. _cohorts.Value, newCohort]);
                              client.Toast("Cohort saved!");
                              _navigateTo("library");
                            })
                        | new Button("Export").Variant(ButtonVariant.Outline).Icon(Icons.Download).Width(Size.Full())
                            .HandleClick(_ => client.Toast("Exporting cohort...")))
                    | (Layout.Horizontal().Gap(2).Width(Size.Full())
                        | new Button("Schedule").Variant(ButtonVariant.Outline).Icon(Icons.Clock).Width(Size.Full())
                            .HandleClick(_ => client.Toast("Schedule dialog coming soon"))
                        | new Button("Clear").Variant(ButtonVariant.Destructive).Icon(Icons.Trash2).Width(Size.Full())
                            .HandleClick(_ => canvasBlocks.Set(Array.Empty<string>()))))
                | new Card(
                    Layout.Vertical().Gap(1).Align(Align.Center)
                        | Text.H2(count > 0 ? count.ToString("N0") : "—")
                        | Text.P("Realtime Count").Muted().Small()
                  ))
    );


    // --- Sidebar block palette ---
    var sidebarContent = Layout.Vertical().Gap(2)
        | AvailableBlocks
            .Where(b => !canvasBlocks.Value.Contains(b.Id))
            .Select(b =>
                (object)new Card(
                    Layout.Horizontal().Gap(3).Align(Align.Center)
                        | (Layout.Vertical()
                            | Text.P(b.Label).Small())
                        | new Spacer()
                        | new Button("+")
                            .Variant(ButtonVariant.Ghost)
                            .HandleClick(_ =>
                                canvasBlocks.Set(canvasBlocks.Value.Append(b.Id).ToArray()))
                ).WithTooltip(b.Description));

    // --- Canvas card ---
    object canvasInner;
    if (canvasBlocks.Value.Length == 0)
    {
      canvasInner = Layout.Center()
          | (Layout.Vertical().Gap(6).Align(Align.Center)
              | new Icon(Icons.LayoutTemplate)
              | Text.H3("No blocks added yet")
              | Text.P("Click '+' on any block in the sidebar to add it here.").Muted());
    }
    else
    {
      canvasInner = Layout.Vertical().Gap(3).Align(Align.Center).Width(Size.Full())
          | canvasBlocks.Value.Select((blockId, idx) =>
          {
            var def = AvailableBlocks.FirstOrDefault(b => b.Id == blockId)
                                  ?? new BlockDef(blockId, blockId, "", Icons.Box, "EVENT");
            var capturedIdx = idx;
            return (object)(Layout.Horizontal().Width(Size.Full()).Align(Align.Center)
              | new Card(
                    Layout.Horizontal().Gap(4).Align(Align.Center).Padding(4)
                        | new Icon(def.Icon).Color(Colors.Blue)
                        | (Layout.Vertical()
                            | Text.P(def.Label).Bold()
                            | Text.P(def.Category).Color(Colors.Purple).Small())
                        | new Spacer()
                        | new Button("✕")
                            .Variant(ButtonVariant.Ghost)
                            .HandleClick(_ =>
                                canvasBlocks.Set(
                                    canvasBlocks.Value.Where((_, i) => i != capturedIdx).ToArray()))
                ).Width(Size.Units(122)));
          });
    }

    var canvasCard = new Card(
        Layout.Vertical().Height(Size.Units(96))
            | canvasInner
    );

    // --- Cohort Insights ---
    object? cohortInsights = null;
    if (canvasBlocks.Value.Length > 0)
    {
      var demoData = new[]
      {
            new { Region = "Asia",  Value = 15 },
            new { Region = "EU",    Value = 25 },
            new { Region = "Other", Value = 10 },
            new { Region = "USA",   Value = 50 },
        };

      var eventsData = new[]
      {
            new { Event = "Purchase",    Count = 180 },
            new { Event = "Sign Up",     Count = 230 },
            new { Event = "Page View",   Count = 1580 },
            new { Event = "Add to Cart", Count = 480 },
        };

      cohortInsights = Layout.Vertical().Gap(4)
          | (Layout.Vertical().Gap(1)
              | Text.H3("Cohort Insights")
              | Text.P("An overview of the users in your defined cohort.").Muted())
          | (Layout.Grid().Columns(2).Gap(4)
              | new Card(
                  Layout.Vertical().Gap(2)
                      | Text.P("Demographic Breakdown").Bold()
                      | demoData.ToPieChart(e => e.Region, e => (double)e.Sum(f => f.Value)))
              | new Card(
                  Layout.Vertical().Gap(2)
                      | Text.P("Top Events").Bold()
                      | eventsData.ToBarChart()
                          .Dimension("Event", e => e.Event)
                          .Measure("Count", e => (double)e.Sum(f => f.Count))));
    }

    // --- Main content: header + canvas + insights ---
    var mainContent = Layout.Vertical().Gap(8)
        | headerCard
        | canvasCard
        | cohortInsights;

    return new SidebarLayout(
        mainContent: mainContent,
        sidebarContent: sidebarContent,
        sidebarHeader: Layout.Vertical().Gap(2) | Text.Lead("Block")
    );
  }
}
