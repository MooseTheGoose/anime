﻿using System;
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
        private bool moveSelectedWithMouse = false;
        private Point mouseOfs;
        private double zoom = 1.0;
        private double zoomSensitivity = 1.0 / 1000.0;

        private DispatcherTimer animationTimer;
        private int index = 0;

        public MainWindow()
        {
            InitializeComponent();
            sourceCanvas.Tag = framesDisplay;
            pdata = new ProjectData();
            frameWidthText.Text = pdata.FrameWidth.ToString();
            frameHeightText.Text = pdata.FrameHeight.ToString();
            animationTimeText.Text = pdata.AnimationTimeMs.ToString();

            animationTimer = new DispatcherTimer();
            animationTimer.Interval = new TimeSpan(pdata.AnimationTimeMs * ProjectData.MsToTicks);
            animationTimer.Tick += AnimationTimerTick;
            animationTimer.Tag = animationDisplay;
            animationTimer.Start();
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

        private void SourceRectangle_MouseLeftButtonDownFunction(object sender, MouseButtonEventArgs e)
        {
            FrameData frame = (sender as Rectangle).Tag as FrameData;
            Canvas c = frame.WinRect.Parent as Canvas;
            mouseOfs = e.GetPosition(c);
            mouseOfs.X /= savedBitmap.Width;
            mouseOfs.Y /= savedBitmap.Height;
            frame.WinRect.CaptureMouse();
            moveSelectedWithMouse = true;
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                pdata.SelectFrame(frame, !frame.Selected);
                moveSelectedWithMouse = false;
            }
            else if(!frame.Selected)
            {
                foreach(FrameData fd in pdata.Frames)
                {
                    pdata.DeselectFrame(fd);                    
                }
                pdata.SelectFrame(frame);
            }
            foreach(FrameData fd in pdata.Frames)
            {
                fd.ResetPrevLeftTop();
            }
            e.Handled = true;
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
            if(moveSelectedWithMouse)
            {
                Canvas c = (sender as Rectangle).Parent as Canvas;
                Point mousePos = e.GetPosition(c);
                foreach(FrameData fd in pdata.Frames.Where(data => data.Selected))
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
            fd.SetNewCoords((int)(normalizedX * savedBitmap.PixelWidth), (int)(normalizedY * savedBitmap.PixelHeight));
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
                if (newdata.Skip)
                {
                    pdata.DisableSkipFrame(newdata);
                    newdata.SkipButton.Content = "Skip";
                }
                else
                {
                    pdata.SkipFrame(newdata);
                    newdata.SkipButton.Content = "Use";
                }
            };

            c.Children.Add(newdata.WinRect);
            (c.Tag as Panel).Children.Add(newdata.InfoPanel);
        }

        private void SourceCanvas_MouseRightDownAddRect(object sender, MouseButtonEventArgs e)
        {
            Canvas c = sender as Canvas;
            Point mousePos = e.GetPosition(c);

            FrameData newdata = pdata.AddFrame();
            SetFrameDataWithNormalizedCoords(newdata, mousePos.X / c.Width, mousePos.Y / c.Height);

            AddFrameData(newdata, c);

            foreach(FrameData fd in pdata.Frames)
            {
                pdata.DeselectFrame(fd);
            }
            pdata.SelectFrame(newdata);
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

        /* More parameters to come... */
        private void SaveAnimation(string fname)
        {
            FrameData[] fdarray = pdata.Frames.ToArray();
            int width = (int)Math.Sqrt(fdarray.Length);
            int height = (fdarray.Length + width - 1) / width;
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

            PngBitmapEncoder encoder = new PngBitmapEncoder();
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
            if(dialog.ShowDialog() ?? false)
            {
                try
                {
                    SaveAnimation(dialog.FileName);
                    MessageBox.Show("File Saved!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void MenuSaveProject_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
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
            this.Close();
        }
    }
}