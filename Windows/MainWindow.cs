using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Win32;

namespace AnimationExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProjectData pdata;
        private BitmapImage savedBitmap;
        private Rectangle boxSelect;
        private bool moveSelectedWithMouse = false;
        private Point mouseOfs;
        private double zoom = 1.0;
        private double zoomSensitivity = 1.0 / 1000.0;

        private readonly string ProjectFilterString = "XML files (*.xml)|*.xml";
        private readonly string ImportImageFilterString = "PNG files (*.png)|*.png|GIF files (*.gif)|*.gif|All files (*.*)|*.*";
        private readonly string ExportImageFilterString = "PNG files (*.png)|*.png";

        private DispatcherTimer animationTimer;
        private int index = 0;

        private class KeyMoveIncrementCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;

            private readonly Action<int, int> exec;
            private readonly int deltax, deltay;

            public KeyMoveIncrementCommand(Action<int, int> moveFunc, int xinc, int yinc)
            {
                deltax = xinc;
                deltay = yinc;
                exec = moveFunc;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                exec(deltax, deltay);
            }
        }

        KeyMoveIncrementCommand[] holdKeyMoves;

        public MainWindow()
        {
            InitializeComponent();
            sourceCanvas.Tag = framesDisplay;
            pdata = new ProjectData();
            frameWidthText.Text = pdata.FrameWidth.ToString();
            frameHeightText.Text = pdata.FrameHeight.ToString();
            animationTimeText.Text = pdata.AnimationTimeMs.ToString();

            animationTimer = new DispatcherTimer(DispatcherPriority.Send);
            animationTimer.Interval = new TimeSpan(pdata.AnimationTimeMs * ProjectData.MsToTicks);
            animationTimer.Tick += AnimationTimerTick;
            animationTimer.Tag = animationDisplay;
            animationTimer.Start();

            boxSelect = new Rectangle();
            boxSelect.Visibility = Visibility.Hidden;
            boxSelect.Opacity = pdata.RectOpacity;
            boxSelect.Fill = pdata.BoxSelectFill;
            boxSelect.Stroke = pdata.BoxSelectStroke;
            sourceCanvas.Children.Add(boxSelect);
            Action<int, int> moveAllFrames = (int deltax, int deltay) =>
            {
                if (savedBitmap != null)
                {
                    foreach (FrameData data in pdata.Frames.Where(data => data.Selected))
                    {
                        int newx = (int)Math.Clamp((long)data.X + deltax, int.MinValue, int.MaxValue);
                        int newy = (int)Math.Clamp((long)data.Y + deltay, int.MinValue, int.MaxValue);
                        SetFrameDataWithNormalizedCoords(data, (double)newx / savedBitmap.PixelWidth, (double)newy / savedBitmap.PixelHeight);
                    }
                }
            };
            holdKeyMoves = new KeyMoveIncrementCommand[4];
            int[] xDeltas = new int[] { -1, 0, 1, 0 };
            int[] yDeltas = new int[] { 0, 1, 0, -1 };
            Key[] arrows = new Key[] { Key.H, Key.J, Key.L, Key.K };
            for (int i = 0; i < 4; i += 1)
            {
                holdKeyMoves[i] = new KeyMoveIncrementCommand(moveAllFrames, xDeltas[i], yDeltas[i]);
                InputBindings.Add(new KeyBinding(holdKeyMoves[i], arrows[i], ModifierKeys.Control));
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Are you sure you want to quit?", "Quit", MessageBoxButton.YesNoCancel);
            if (res != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void SetFrameDimensions(int w, int h)
        {
            if (w > 0 && h > 0)
            {
                pdata.FrameWidth = w;
                pdata.FrameHeight = h;
                foreach (FrameData fd in pdata.Frames)
                {
                    fd.WinRect.Width = w * (savedBitmap.Width / savedBitmap.PixelWidth);
                    fd.WinRect.Height = h * (savedBitmap.Height / savedBitmap.PixelHeight);
                }
            }
        }

        private void SetAnimationTimerMs(int ms)
        {
            if(ms > 0 && ms <= ProjectData.MaxMsTime)
            {
                pdata.AnimationTimeMs = ms;
                if(animationTimer != null)
                {
                    animationTimer.Stop();
                    animationTimer.Interval = new TimeSpan(pdata.AnimationTimeMs * ProjectData.MsToTicks);
                    animationTimer.Start();
                }
            }
        }

        private void SetZoom(double zoom)
        {
            this.zoom = zoom;
            if (savedBitmap != null)
            {
                sourceCanvas.Width = savedBitmap.Width * zoom;
                sourceCanvas.Height = savedBitmap.Height * zoom;
                sourceCanvas.RenderTransform = new ScaleTransform(zoom, zoom);
            }
        }

        private void FrameWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            int w;
            if(int.TryParse((sender as TextBox).Text, out w))
            {
                SetFrameDimensions(w, pdata.FrameHeight);      
            }
        }

        private void FrameHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            int h;
            if(int.TryParse((sender as TextBox).Text, out h))
            {
                SetFrameDimensions(pdata.FrameWidth, h);
            }
        }

        private void AnimationTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            int ms;
            if(int.TryParse((sender as TextBox).Text, out ms))
            {
                SetAnimationTimerMs(ms);
            }
        }

        private void AnimationTimerTick(object sender, EventArgs e)
        {
            FrameData[] fdarray = pdata.Frames.Where(data => !data.Skip).ToArray();
            if(sourceImage != null && savedBitmap != null && fdarray.Length > 0) 
            {
                index = (index + 1) % fdarray.Length;
                FrameData nextframe = fdarray[index];
                ((sender as DispatcherTimer).Tag as Image).Source = pdata.RenderFrameDataToBitmap(savedBitmap, nextframe);
            }
        }

        private void SourceScroller_CtrlScrollZoomFunction(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                zoom *= Math.Pow(2.0, e.Delta * zoomSensitivity);
                SetZoom(zoom);
                e.Handled = true;
            }
        }

        private void RemoveFrameData(FrameData fd)
        {
            pdata.Frames.Remove(fd);
            (fd.WinRect.Parent as Canvas).Children.Remove(fd.WinRect);
            (fd.InfoPanel.Parent as Panel).Children.Remove(fd.InfoPanel);
        }

        private void SourceScroller_KeyDownFunction(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.X)
            {
                foreach(FrameData fd in pdata.Frames.ToArray().Where(data => data.Selected))
                {
                    RemoveFrameData(fd);
                }
            }
        }

        private bool ToggleShortcut()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        private bool BoxShortcut()
        {
            return (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.B);
        }

        private bool SkipShortcut() 
        {
            return (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.S);
        }

        private bool DeleteShortcut()
        {
            return (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.D);
        }

        private void SelectOrSkipFrame(FrameData data, bool truth)
        {
            if (SkipShortcut())
            {
                pdata.SkipFrame(data, truth);
            }
            else
            {
                pdata.SelectFrame(data, truth);
            }
        }

        private void SelectOrSkipFrame(FrameData data)
        {
            SelectOrSkipFrame(data, true);
        }

        private void SourceCanvas_MouseLeftDownSelectRect(object sender, MouseButtonEventArgs e)
        {
            Canvas c = sender as Canvas;
            mouseOfs = e.GetPosition(c);
            if (BoxShortcut())
            {
                c.CaptureMouse();
                Canvas.SetLeft(boxSelect, mouseOfs.X);
                Canvas.SetTop(boxSelect, mouseOfs.Y);
                boxSelect.Visibility = Visibility.Visible;
            }
            else
            {
                foreach (FrameData data in pdata.Frames)
                {
                    SelectOrSkipFrame(data, false);
                }
            }
            e.Handled = true;
        }

        private void SourceCanvas_MouseLeftUpCompleteSelect(object sender, MouseButtonEventArgs e)
        {
            if (boxSelect.Visibility == Visibility.Visible)
            {
                bool doDelete = DeleteShortcut();
                bool doDeselect = !doDelete && !ToggleShortcut();
                foreach (FrameData data in pdata.Frames.ToArray())
                {
                    if (doDeselect)
                    {
                        SelectOrSkipFrame(data, false);
                    }
                    Rect fdrect = new Rect(Canvas.GetLeft(data.WinRect), Canvas.GetTop(data.WinRect), data.WinRect.Width, data.WinRect.Height);
                    Rect boxrect = new Rect(Canvas.GetLeft(boxSelect), Canvas.GetTop(boxSelect), boxSelect.Width, boxSelect.Height);
                    if (fdrect.IntersectsWith(boxrect))
                    {
                        if (doDelete)
                        {
                            RemoveFrameData(data);
                        }
                        else
                        {
                            SelectOrSkipFrame(data);
                        }
                    }
                }
                boxSelect.Visibility = Visibility.Hidden;
            }
            (sender as Canvas).ReleaseMouseCapture();
        }

        private void SourceCanvas_MouseMoveSelect(object sender, MouseEventArgs e)
        {
            Point currMouse = e.GetPosition(sender as Canvas);
            Canvas.SetLeft(boxSelect, Math.Min(currMouse.X, mouseOfs.X));
            Canvas.SetTop(boxSelect, Math.Min(currMouse.Y, mouseOfs.Y));
            boxSelect.Width = Math.Abs(currMouse.X - mouseOfs.X);
            boxSelect.Height = Math.Abs(currMouse.Y - mouseOfs.Y);
        }

        private void SourceRectangle_MouseLeftButtonDownFunction(object sender, MouseButtonEventArgs e)
        {
            FrameData frame = (sender as Rectangle).Tag as FrameData;
            Canvas c = frame.WinRect.Parent as Canvas;
            mouseOfs = e.GetPosition(c);
            mouseOfs.X /= savedBitmap.Width;
            mouseOfs.Y /= savedBitmap.Height;

            if (!BoxShortcut())
            {
                if (DeleteShortcut())
                {
                    RemoveFrameData(frame);
                }
                else
                {
                    moveSelectedWithMouse = !SkipShortcut();
                    bool concern = !moveSelectedWithMouse ? frame.Skip : frame.Selected;
                    frame.WinRect.CaptureMouse();
                    if (ToggleShortcut())
                    {
                        SelectOrSkipFrame(frame, !concern);
                        moveSelectedWithMouse = false;
                    }
                    else if (!concern)
                    {
                        foreach (FrameData fd in pdata.Frames)
                        {
                            SelectOrSkipFrame(fd, false);
                        }
                        SelectOrSkipFrame(frame);
                    }
                    foreach (FrameData fd in pdata.Frames)
                    {
                        fd.ResetPrevLeftTop();
                    }
                }
                e.Handled = true;
            }
        }

        private void SourceRectangle_MouseLeftButtonUpFunction(object sender, MouseButtonEventArgs e)
        {
            FrameData frame = (sender as Rectangle).Tag as FrameData;
            frame.WinRect.ReleaseMouseCapture();
            moveSelectedWithMouse = false;
            e.Handled = true;
        }

        private void SourceRectangle_MouseMoveFunction(object sender, MouseEventArgs e)
        {
            Canvas c = (sender as Rectangle).Parent as Canvas;
            Point mousePos = e.GetPosition(c);
            if (moveSelectedWithMouse)
            {
                foreach (FrameData fd in pdata.Frames.Where(data => data.Selected))
                {
                    double normalX = fd.PrevLeft / savedBitmap.Width + mousePos.X / savedBitmap.Width - mouseOfs.X;
                    double normalY = fd.PrevTop / savedBitmap.Height + mousePos.Y / savedBitmap.Height - mouseOfs.Y;
                    SetFrameDataWithNormalizedCoords(fd, normalX, normalY);
                }
                e.Handled = true;
            }
        }

        private void SetFrameDataWithNormalizedCoords(FrameData fd, double normalizedX, double normalizedY)
        {
            fd.SetNewCoords((int)Math.Round(normalizedX * savedBitmap.PixelWidth), (int)Math.Round(normalizedY * savedBitmap.PixelHeight));
            Canvas.SetLeft(fd.WinRect, normalizedX * savedBitmap.Width);
            Canvas.SetTop(fd.WinRect, normalizedY * savedBitmap.Height);
        }

        private void AddFrameData(FrameData newdata, Canvas c)
        {
            newdata.WinRect.MouseLeftButtonDown += SourceRectangle_MouseLeftButtonDownFunction;
            newdata.WinRect.MouseLeftButtonUp += SourceRectangle_MouseLeftButtonUpFunction;
            newdata.WinRect.MouseMove += SourceRectangle_MouseMoveFunction;
            newdata.WinRect.Width = pdata.FrameWidth * (savedBitmap.Width / savedBitmap.PixelWidth);
            newdata.WinRect.Height = pdata.FrameHeight * (savedBitmap.Height / savedBitmap.PixelHeight);

            newdata.DeleteButton.Click += (object sender, RoutedEventArgs e) => {
                RemoveFrameData(newdata);
            };
            newdata.SkipButton.Click += (object sender, RoutedEventArgs e) => {
                pdata.SkipFrame(newdata, !newdata.Skip);
            };

            c.Children.Add(newdata.WinRect);
            (c.Tag as Panel).Children.Add(newdata.InfoPanel);
        }

        private void SourceCanvas_MouseRightDownAddRect(object sender, MouseButtonEventArgs e)
        {
            Canvas c = sender as Canvas;
            Point mousePos = e.GetPosition(c);

            FrameData newdata = pdata.AddFrame();
            SetFrameDataWithNormalizedCoords(newdata, mousePos.X / savedBitmap.Width, mousePos.Y / savedBitmap.Height);

            AddFrameData(newdata, c);

            if (!ToggleShortcut())
            {
                foreach (FrameData fd in pdata.Frames)
                {
                    pdata.DeselectFrame(fd);
                }
            }
            pdata.SelectFrame(newdata);
            if (SkipShortcut())
            {
                pdata.SkipFrame(newdata);
            }
        }

        private void LoadNewProjectImage(string fname)
        {
            BitmapImage bi = new BitmapImage(new Uri(fname));
            if(bi.PixelWidth <= 0 || bi.PixelHeight <= 0)
            {
                throw new ApplicationException("Size of image must be greater than 0.");
            }

            sourceImage.Source = bi;
            savedBitmap = bi;
            SetZoom(1.0);
            pdata.ImagePath = fname;
        }

        private void SaveAnimation(string fname, int width, int height)
        {
            FrameData[] fdarray = pdata.Frames.ToArray();
            DrawingVisual vis = new DrawingVisual();
            using (DrawingContext ctx = vis.RenderOpen())
            {
                for (int i = 0; i < fdarray.Length; i += 1)
                {
                    int x = i % width;
                    int y = i / width;
                    ctx.DrawImage(pdata.RenderFrameDataToBitmap(savedBitmap, fdarray[i]),
                        new Rect(pdata.FrameWidth * x, pdata.FrameWidth * y,
                                 pdata.FrameWidth, pdata.FrameHeight));
                }
            }
            RenderTargetBitmap texture = new RenderTargetBitmap(width * pdata.FrameWidth, height * pdata.FrameHeight, 96, 96, PixelFormats.Pbgra32);
            texture.Render(vis);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(texture));
            using (Stream stream = File.Create(fname))
            {
                encoder.Save(stream);
            }
        }

        private void MenuImportImage_Click(object sender, RoutedEventArgs e) 
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = ImportImageFilterString;
            if(dialog.ShowDialog() ?? false)
            {
                try
                {
                    LoadNewProjectImage(dialog.FileName);
                    foreach (FrameData fd in pdata.Frames.ToArray())
                    {
                        RemoveFrameData(fd);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void MenuExportImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = ExportImageFilterString;
            if (pdata.Frames.Count <= 0)
            {
                MessageBox.Show("Cannot export animation with 0 frames");
            }
            else if(dialog.ShowDialog() ?? false)
            {
                AnimationSaveDialog animDialog = new AnimationSaveDialog(pdata.Frames.Count);
                if (animDialog.ShowDialog() ?? false)
                {
                    try
                    {
                        SaveAnimation(dialog.FileName, animDialog.AnimationWidth, animDialog.AnimationHeight);
                        MessageBox.Show("File Saved!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void MenuSaveProject_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = ProjectFilterString;
            if (dialog.ShowDialog() ?? false)
            {
                try
                {
                    pdata.SaveProjectData(dialog.FileName);
                    MessageBox.Show("Project Saved!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void MenuLoadProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = ProjectFilterString;
            if (dialog.ShowDialog() ?? false)
            {
                try
                {
                    FrameData[] prior = pdata.Frames.ToArray();
                    pdata.LoadProjectData(dialog.FileName);
                    LoadNewProjectImage(pdata.ImagePath);
                    SetFrameDimensions(pdata.FrameWidth, pdata.FrameHeight);
                    foreach (FrameData fd in pdata.Frames)
                    {
                        AddFrameData(fd, sourceCanvas);
                        Canvas.SetLeft(fd.WinRect, fd.X * savedBitmap.Width / savedBitmap.PixelWidth);
                        Canvas.SetTop(fd.WinRect, fd.Y * savedBitmap.Height / savedBitmap.PixelHeight);
                    }
                    SetAnimationTimerMs(pdata.AnimationTimeMs);
                    foreach (FrameData fd in prior)
                    {
                        sourceCanvas.Children.Remove(fd.WinRect);
                        (sourceCanvas.Tag as Panel).Children.Remove(fd.InfoPanel);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
