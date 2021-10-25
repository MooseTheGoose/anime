using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;

namespace AnimationExtractor
{
    public class FrameData
    {
        public Rectangle WinRect;
        public DockPanel InfoPanel;
        public int X;
        public int Y;
        public double PrevLeft;
        public double PrevTop;
	    public bool Selected;
        public bool Skip;
        public TextBox TextX;
        public TextBox TextY;
        public Button XUp;
        public Button XDown;
        public Button YUp;
        public Button YDown;
        public Button SkipButton;
        public Button DeleteButton;

        public void SetNewCoords(int fx, int fy)
        {
            X = fx;
            Y = fy;
            TextX.Text = X.ToString();
            TextY.Text = Y.ToString();
        }

        public void ResetPrevLeftTop()
        {
            PrevLeft = Canvas.GetLeft(WinRect);
            PrevTop = Canvas.GetTop(WinRect);
        }
       
        public FrameData()
        {
            TextBlock LabelX;
            TextBlock LabelY;

            WinRect = new Rectangle();
            InfoPanel = new DockPanel();

            WinRect.Tag = this;
            Selected = true;
            Skip = false;

            LabelX = new TextBlock();
            LabelX.Text = " X: ";
            TextX = new TextBox();
            TextX.IsReadOnly = true;

            LabelY = new TextBlock();
            LabelY.Text = " Y: ";
            TextY = new TextBox();
            TextY.IsReadOnly = true;

            SetNewCoords(0, 0);
            ResetPrevLeftTop();

            SkipButton = new Button();
            SkipButton.Content = "Skip";

            DeleteButton = new Button();
            DeleteButton.Content = " X ";

            Separator sep = new Separator();
            sep.Background = Brushes.Transparent;
            DockPanel.SetDock(LabelX, Dock.Left);
            DockPanel.SetDock(TextX, Dock.Left);
            DockPanel.SetDock(LabelY, Dock.Left);
            DockPanel.SetDock(TextY, Dock.Left);
            DockPanel.SetDock(SkipButton, Dock.Right);
            DockPanel.SetDock(DeleteButton, Dock.Right);
            DockPanel.SetDock(sep, Dock.Right);

            InfoPanel.Children.Add(LabelX);
            InfoPanel.Children.Add(TextX);
            InfoPanel.Children.Add(LabelY);
            InfoPanel.Children.Add(TextY);
            InfoPanel.Children.Add(SkipButton);
            InfoPanel.Children.Add(DeleteButton);
            InfoPanel.Children.Add(sep);
        }
    }
}