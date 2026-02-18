namespace Frontend.Apps;

public class BuilderView : ViewBase
{
  record BlockDef(string Id, string Label, string Description, string Icon, string Category);

  static readonly BlockDef[] AvailableBlocks =
  {
        new("user_signup",       "User Sign Up",      "Fired when a user registers",        "UserPlus",     "EVENT"),
        new("purchase",          "Purchase",          "Fired when a purchase is completed", "ShoppingCart", "EVENT"),
        new("page_view",         "Page Views",        "Fired on each page visit",           "Eye",          "EVENT"),
        new("country",           "Country",           "Filter by user country",             "Globe",        "FILTER"),
        new("device_type",       "Device Type",       "Filter by device (mobile/desktop)",  "Monitor",       "FILTER"),
        new("onboarding_funnel", "Onboarding Funnel", "Tracks onboarding step completion",  "ListChecks",   "EVENT"),
    };

  static Icons ParseIcon(string name) => name switch
  {
    "UserPlus" => Icons.UserPlus,
    "ShoppingCart" => Icons.ShoppingCart,
    "Eye" => Icons.Eye,
    "Globe" => Icons.Globe,
    "Monitor" => Icons.Monitor,
    "ListChecks" => Icons.ListChecks,
    _ => Icons.Box,
  };

  public override object? Build()
  {
    var canvasBlocks = UseState(Array.Empty<string>());
    var client = UseService<IClientProvider>();

    // Realtime count (number of blocks added as a proxy)
    int count = canvasBlocks.Value.Length * 1247; // simulated count

    var headerCard = new Card(
        Layout.Vertical().Gap(4).Width(Size.Full())
            | (Layout.Vertical().Gap(1)
                | Text.H3("New Prospect Cohort")
                | Text.P("Define your target audience by combining filters.").Muted())
            | (Layout.Horizontal().Gap(4).Align(Align.Center).Width(Size.Full())
                | (Layout.Vertical().Gap(2)
                    | (Layout.Horizontal().Gap(2).Width(Size.Full())
                        | new Button("Save Cohort").Variant(ButtonVariant.Primary).Icon(Icons.Save).Width(Size.Full())
                            .HandleClick(_ => client.Toast("Cohort saved!"))
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
        | AvailableBlocks.Select(b =>
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
                                  ?? new BlockDef(blockId, blockId, "", "Box", "EVENT");
            var capturedIdx = idx;
            return (object)(Layout.Horizontal().Width(Size.Full()).Align(Align.Center)
              | new Card(
                    Layout.Horizontal().Gap(4).Align(Align.Center).Padding(4)
                        | new Icon(ParseIcon(def.Icon)).Color(Colors.Blue)
                        | (Layout.Vertical()
                            | Text.P(def.Label).Bold()
                            | Text.P(def.Category).Color(Colors.Purple).Small())
                        | new Spacer()
                        | new Button("✕")
                            .Variant(ButtonVariant.Ghost)
                            .HandleClick(_ =>
                                canvasBlocks.Set(
                                    canvasBlocks.Value.Where((_, i) => i != capturedIdx).ToArray()))
                ).Width(Size.Units(102)));
          });
    }

    var canvasCard = new Card(
        Layout.Vertical().Height(Size.Units(136))
            | canvasInner
    );

    // --- Main content: header + canvas ---
    var mainContent = Layout.Vertical().Gap(4)
        | headerCard
        | canvasCard;

    return new SidebarLayout(
        mainContent: mainContent,
        sidebarContent: sidebarContent,
        sidebarHeader: Layout.Vertical().Gap(2) | Text.Lead("Block")
    );
  }
}
