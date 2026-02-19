using Ivy.Shared;

namespace Frontend.Apps;

public class DashboardHeader(IState<int> selectedTab, Action onImportClick) : ViewBase
{
  public override object Build()
  {
    return Layout.Horizontal().Align(Align.Center).Width(Size.Full()).Gap(10)
        .Padding(10, 6)
        .Add(Text.H3("Behavioral Ops").NoWrap())
        .Add(Layout.Horizontal().Gap(1)
            .Add(RenderTabButton("Builder", 4))
            .Add(RenderTabButton("Library", 1))
            .Add(RenderTabButton("Dashboard", 0))
            .Add(RenderTabButton("Testing", 2))
        )
        .Add(new Spacer())
        .Add(new Button("New Cohort", onImportClick)
            .Icon(Icons.Plus)
            .Variant(ButtonVariant.Primary));
  }

  private object RenderTabButton(string label, int index)
  {
    var isActive = selectedTab.Value == index;
    return new Button(label, () => selectedTab.Set(index))
        .Variant(isActive ? ButtonVariant.Secondary : ButtonVariant.Ghost);
  }
}
