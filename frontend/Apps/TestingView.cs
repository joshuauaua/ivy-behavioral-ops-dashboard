namespace Frontend.Apps;

public class TestingView : ViewBase
{
  public override object? Build()
  {
    return Layout.Center()
        | Layout.Vertical().Gap(4).Align(Align.Center)
            | Text.H2("Testing")
            | Text.P("Validate and test your cohort filters against user data.");
  }
}
