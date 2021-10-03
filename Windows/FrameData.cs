using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

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
            WinRect = new Rectangle();
            InfoPanel = new DockPanel();

            WinRect.Tag = this;
            Selected = true;
            Skip = false;

            TextX = new TextBox();
            TextX.IsReadOnly = true;

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

            DockPanel.SetDock(TextX, Dock.Left);
            DockPanel.SetDock(TextY, Dock.Left);
            DockPanel.SetDock(SkipButton, Dock.Right);
            DockPanel.SetDock(DeleteButton, Dock.Right);
            DockPanel.SetDock(sep, Dock.Right);

            InfoPanel.Children.Add(TextX);
            InfoPanel.Children.Add(TextY);
            InfoPanel.Children.Add(SkipButton);
            InfoPanel.Children.Add(DeleteButton);
            InfoPanel.Children.Add(sep);
        }
    }
}